"""Extend the historic-figures reference database from ~10 per cell to ~1000 per cell (49 cells -> 49,000).

  python expand_historic_figures.py discover   # Wikidata -> candidates.json (per cell: QID + sitelink count)
  python expand_historic_figures.py select     # candidates.json -> picked.json (one cell per person, top N per cell, shortfall redistributed)
  python expand_historic_figures.py facts      # picked.json -> facts.json (label, dob, place, coords, country, sex, dod)
  python expand_historic_figures.py build      # picked + facts -> expanded.json + expand_report.md (GeoArea proxy applied)
  python expand_historic_figures.py load       # expanded.json -> tbl_Ref_HistoricFigure (idempotent upsert; needs migration 181)

Same rule as build_historic_figures.py: nothing is invented. Discovery only decides *who* belongs in a cell (by
Wikidata occupation / position held / religion), ranked by number of Wikipedia language editions (sitelinks) as a
notability measure. Every stored date, place and coordinate comes from the person's Wikidata item, and a person is
kept only if the birth date has day precision. Birth time is never stored (Wikidata has none we can trust).

GeoArea ("reach of influence") is NOT a Wikidata fact. It is a documented proxy from sitelink count and birth country
(see GEO_TIERS); the count itself is stored in SiteLinkCount so the proxy stays auditable.
"""

from __future__ import annotations

import json
import sys
import time
import urllib.parse
import urllib.request
from collections import Counter, defaultdict
from pathlib import Path

HERE = Path(__file__).parent
CANDIDATES, PICKED, FACTS, EXPANDED, REPORT = (HERE / n for n in
                                              ("candidates.json", "picked.json", "facts.json", "expanded.json", "expand_report.md"))
CURATED = HERE / "selected.json"                # the 488 hand-seeded people: kept as they are, counted toward their cell
UA = "ikiastrro-historic-figures/0.2 (research; rammyps23@gmail.com)"
PER_CELL = 1000
MIN_LINKS = 6          # floor on Wikipedia editions; keeps stubs out and every query fast
PAGE = 20000

INDIA = "wd:Q668 wd:Q129286"                    # India, British Raj (undivided-India births)
# ---- atoms: a SPARQL fragment about ?p (a human). Fragments in one cell are UNIONed.
def occ(*q): return "?p wdt:P106 ?o . VALUES ?o { %s }" % " ".join("wd:" + x for x in q)
def pos(*q): return "?p wdt:P39 ?pos . ?pos wdt:P279* ?pc . VALUES ?pc { %s }" % " ".join("wd:" + x for x in q)
def rel(r, *q): return "?p wdt:P106 ?o . VALUES ?o { %s } ?p wdt:P140 wd:%s" % (" ".join("wd:" + x for x in q), r)
def founder(*industry): return "?c wdt:P112 ?p ; wdt:P452 ?i . VALUES ?i { %s }" % " ".join("wd:" + x for x in industry)
def indian(f): return f + " ?p wdt:P27 ?cit . VALUES ?cit { %s }" % INDIA

RELIGIOUS = ("Q15995642", "Q177826", "Q1234713", "Q4479442")     # religious leader, clergy, theologian, founder

# Cell order = assignment priority: a person goes to the FIRST cell that wants them (specific before general).
CELLS: list[tuple[str, str, list[str]]] = [
    ("IND", "PMIN", [indian(pos("Q14212"))]),
    ("IND", "CM", [indian(pos("Q5096847", "Q1670755"))]),
    ("IND", "PRESGOV", [indian(pos("Q30461", "Q132050"))]),
    ("IND", "FFIGHT", [indian(occ("Q11571698", "Q3242115"))]),
    ("IND", "CONST", [indian(occ("Q185351", "Q16533", "Q40348"))]),
    ("IND", "PRINCE", [indian(occ("Q116", "Q1163330")), indian(pos("Q116"))]),
    ("IND", "REFORM", [indian(occ("Q16611574", "Q15253558"))]),
    ("POL", "PRES", [pos("Q30461")]),
    ("POL", "PM", [pos("Q14212")]),
    ("POL", "MON", [occ("Q116"), pos("Q116")]),
    ("POL", "FREEDOM", [occ("Q11571698")]),
    ("POL", "REVOL", [occ("Q3242115")]),
    ("POL", "DICT", [occ("Q183318", "Q180095")]),               # dictator, tyrant
    ("POL", "DIPL", [occ("Q193391")]),
    ("MIL", "ADM", [occ("Q132851")]),
    ("MIL", "GEN", [occ("Q83460")]),
    ("MIL", "RESIST", [occ("Q212948", "Q1397808")]),           # partisan, resistance fighter
    ("MIL", "INTEL", [occ("Q5121444", "Q9352089")]),
    ("MIL", "STRAT", [occ("Q12806039")]),
    ("MIL", "CONQ", [occ("Q126236", "Q220098")]),             # conquistador, warlord
    ("MIL", "WARLEAD", [occ("Q1402561", "Q47064", "Q189290", "Q4991371")]),
    ("SPI", "SIKHJAIN", [rel("Q9316", *RELIGIOUS, "Q2719071"), rel("Q9232", *RELIGIOUS)]),
    ("SPI", "BUDD", [occ("Q854997"), rel("Q748", *RELIGIOUS)]),
    ("SPI", "ISLAM", [occ("Q125482", "Q189459", "Q3155377"), rel("Q432", *RELIGIOUS)]),
    ("SPI", "HINDU", [occ("Q484260", "Q3470892", "Q2901587", "Q267658"), rel("Q9089", *RELIGIOUS)]),
    ("SPI", "CHRIST", [occ("Q19546", "Q29182", "Q45722", "Q250867", "Q611644", "Q1234713", "Q55737401")]),
    ("SPI", "MYSTIC", [occ("Q12328016")]),
    ("SPI", "FOUNDER", [occ("Q15995642", "Q177826")]),
    ("SCI", "ASTRO", [occ("Q11063", "Q155647")]),
    ("SCI", "MED", [occ("Q39631")]),
    ("SCI", "ECON", [occ("Q188094", "Q2306091")]),
    ("SCI", "PHIL", [occ("Q4964182")]),
    ("SCI", "EDU", [occ("Q974144", "Q1231865")]),
    ("SCI", "PHYS", [occ("Q169470", "Q170790")]),
    ("SCI", "INVENT", [occ("Q205375", "Q81096", "Q1709010")]),
    ("BUS", "TECH", [founder("Q11661", "Q638608", "Q75")]),
    ("BUS", "AUTO", [founder("Q190117", "Q862571", "Q2151621", "Q5358497")]),
    ("BUS", "MEDIA", [occ("Q2516866"), founder("Q11033", "Q1331793")]),
    ("BUS", "RETAIL", [founder("Q126793", "Q484847", "Q216107")]),
    ("BUS", "INVEST", [occ("Q557880", "Q1979607", "Q806798")]),
    ("BUS", "PHIL", [occ("Q12362622")]),
    ("BUS", "INDUS", [occ("Q6606110", "Q484876", "Q131524", "Q43845")]),
    ("ART", "CRICK", [occ("Q12299841")]),
    ("ART", "DIRECT", [occ("Q2526255", "Q3282637")]),
    ("ART", "ACTOR", [occ("Q10800557", "Q33999", "Q2259451")]),
    ("ART", "MUSIC", [occ("Q639669", "Q177220", "Q36834")]),
    ("ART", "AUTHOR", [occ("Q36180", "Q49757", "Q482980")]),
    ("ART", "JOURN", [occ("Q1930187", "Q947873", "Q2722764")]),
    ("ART", "ATHLETE", [occ("Q937857", "Q10833314", "Q3665646", "Q11338576", "Q10873124", "Q11513337", "Q10843402",
                            "Q11303721", "Q2309784", "Q378622", "Q10871364", "Q11774891", "Q13382576", "Q2066131")]),
]
assert len(CELLS) == 49 and len({(a, s) for a, s, _ in CELLS}) == 49

# GeoArea proxy by sitelinks (Wikipedia language editions) and birth country. Non-Indian: GG, GC, GR. Indian: IG, IR, IS, IL.
GEO_TIERS_GLOBAL = [(120, "GL_GLOBAL"), (60, "GL_CONT"), (0, "GL_REGION")]
GEO_TIERS_INDIA = [(60, "IN_GLOBAL"), (30, "IN_REGION"), (16, "IN_STATE"), (0, "IN_LOCAL")]


def http_json(url: str, data: bytes | None = None, accept: str = "application/json") -> dict:
    for attempt in range(5):
        try:
            request = urllib.request.Request(url, data=data, headers={"User-Agent": UA, "Accept": accept})
            with urllib.request.urlopen(request, timeout=90) as response:
                return json.load(response)
        except Exception as ex:
            if attempt == 4:
                raise
            print(f"    retry ({type(ex).__name__}: {str(ex)[:60]})", flush=True)
            time.sleep(5 * (attempt + 1))
    raise RuntimeError("unreachable")


def sparql(query: str) -> list[dict]:
    result = http_json("https://query.wikidata.org/sparql", urllib.parse.urlencode({"query": query}).encode(),
                       "application/sparql-results+json")
    return result["results"]["bindings"]


def discover() -> None:
    out: dict[str, dict] = json.loads(CANDIDATES.read_text("utf-8")) if CANDIDATES.exists() else {}
    for area, sub, atoms in CELLS:
        key = f"{area}/{sub}"
        if key in out and out[key]["done"]:
            continue
        found: dict[str, int] = {}
        for i, atom in enumerate(atoms):
            query = f"""
            SELECT ?p (MAX(?l) AS ?links) WHERE {{
              ?p wdt:P31 wd:Q5 ; wikibase:sitelinks ?l . FILTER(?l >= {MIN_LINKS})
              {atom}
              ?p p:P569/psv:P569 ?n . ?n wikibase:timePrecision ?prec . FILTER(?prec >= 11)
            }} GROUP BY ?p ORDER BY DESC(?links) LIMIT {PAGE}"""
            try:
                rows = sparql(query)
            except Exception as ex:
                print(f"  {key} atom {i}: FAILED {type(ex).__name__}: {str(ex)[:80]}", flush=True)
                continue
            for r in rows:
                found[r["p"]["value"].rsplit("/", 1)[1]] = int(r["links"]["value"])
            print(f"  {key} atom {i}: {len(rows)} rows (cell total {len(found)})", flush=True)
            time.sleep(1.5)
        out[key] = {"people": found, "done": True}
        CANDIDATES.write_text(json.dumps(out), "utf-8")
    print("discover done:", {k: len(v["people"]) for k, v in out.items()})


TOTAL = 49 * PER_CELL
MAX_CELL = 3 * PER_CELL                          # a cell never grows past 3x the base even when topping up


def select() -> None:
    cand = json.loads(CANDIDATES.read_text("utf-8"))
    curated = json.loads(CURATED.read_text("utf-8")) if CURATED.exists() else []
    taken = {r["qid"] for r in curated}
    kept = Counter((r["area"], r["sub"]) for r in curated)
    pool: dict[tuple[str, str], list[tuple[str, int]]] = {}
    for area, sub, _ in CELLS:                   # CELLS order = priority; first cell to claim a person keeps them
        entries = sorted(cand.get(f"{area}/{sub}", {"people": {}})["people"].items(), key=lambda kv: -kv[1])
        mine = []
        for qid, links in entries:
            if qid in taken:
                continue
            taken.add(qid)
            mine.append((qid, links))
        pool[(area, sub)] = mine
    quota = {c: max(0, PER_CELL - kept[c]) for c in pool}
    take = {c: min(quota[c], len(pool[c])) for c in pool}
    short = sum(quota[c] - take[c] for c in pool)
    while short > 0:                             # top up from cells with surplus, one person at a time, biggest pool first
        grew = False
        for c in sorted(pool, key=lambda c: len(pool[c]) - take[c], reverse=True):
            if short == 0:
                break
            if take[c] < len(pool[c]) and kept[c] + take[c] < MAX_CELL:
                take[c] += 1
                short -= 1
                grew = True
        if not grew:
            break
    picked = [{"area": a, "sub": s, "qid": q, "links": l} for (a, s), people in pool.items() for q, l in people[:take[(a, s)]]]
    PICKED.write_text(json.dumps(picked), "utf-8")
    print(f"picked {len(picked)} new + {len(curated)} curated = {len(picked) + len(curated)} (target {TOTAL}); unmet {short}")


def facts_stage() -> None:
    from build_historic_figures import facts, to_record      # reuse the audited fact fetch / date conversion
    picked = json.loads(PICKED.read_text("utf-8"))
    have: dict[str, dict] = json.loads(FACTS.read_text("utf-8")) if FACTS.exists() else {}
    todo = [p["qid"] for p in picked if p["qid"] not in have]
    print(f"fetching facts for {len(todo)} people ({len(have)} cached)")
    for i in range(0, len(todo), 500):
        chunk = todo[i:i + 500]
        for qid, f in facts(chunk).items():
            record = to_record(qid, f)
            have[qid] = record or {"skip": True}
        for qid in chunk:
            have.setdefault(qid, {"skip": True})
        FACTS.write_text(json.dumps(have, ensure_ascii=False), "utf-8")
        print(f"  {min(i + 500, len(todo))}/{len(todo)}", flush=True)


def geo_for(links: int, india: bool) -> str:
    for floor, code in (GEO_TIERS_INDIA if india else GEO_TIERS_GLOBAL):
        if links >= floor:
            return code
    raise AssertionError


def build() -> None:
    picked = json.loads(PICKED.read_text("utf-8"))
    have = json.loads(FACTS.read_text("utf-8"))
    cand = json.loads(CANDIDATES.read_text("utf-8"))
    links_of = {q: l for c in cand.values() for q, l in c["people"].items()}
    curated = json.loads(CURATED.read_text("utf-8")) if CURATED.exists() else []
    out, dropped = [], Counter()
    for p in picked:
        r = have.get(p["qid"])
        if not r or r.get("skip"):
            dropped[(p["area"], p["sub"])] += 1
            continue
        india = p["area"] == "IND" or (r["country"] in ("India", "British Raj"))
        out.append({**r, "area": p["area"], "sub": p["sub"], "links": p["links"], "geo": geo_for(p["links"], india)})
    for r in curated:                                              # curated rows keep their hand-assigned geo; just add links
        out.append({**r, "links": links_of.get(r["qid"]), "curated": True})
    EXPANDED.write_text(json.dumps(out, ensure_ascii=False), "utf-8")
    by_cell, by_geo = Counter((r["area"], r["sub"]) for r in out), Counter(r["geo"] for r in out)
    lines = ["# Historic figures expansion report\n", f"Total **{len(out)}** people (target {TOTAL}); "
             f"{sum(dropped.values())} picked candidates dropped (no day-precision birth date once facts were read).\n",
             "## People per cell\n", "| Area | SubCategory | People |", "|---|---|---|"]
    lines += [f"| {a} | {s} | {by_cell[(a, s)]} |" for a, s, _ in CELLS]
    lines += ["\n## GeoArea distribution (proxy: sitelinks + birth country)\n", "| GeoArea | People |", "|---|---|"]
    lines += [f"| {g} | {by_geo[g]} |" for g in ("IN_LOCAL", "IN_STATE", "IN_REGION", "IN_GLOBAL", "GL_REGION", "GL_CONT", "GL_GLOBAL")]
    lines += ["\n## Without birthplace coordinates\n", f"{sum(1 for r in out if r.get('lat') is None)} people."]
    lines += ["\n## Julian-calendar births converted to Gregorian\n", f"{sum(1 for r in out if r.get('calendar') == 'JULIAN')} people."]
    REPORT.write_text("\n".join(lines) + "\n", "utf-8")
    print(f"built {len(out)}; geo {dict(by_geo)}")


def load() -> None:
    from ikiastrro_analytics.sql import connect
    people = json.loads(EXPANDED.read_text("utf-8"))
    sql = """
        MERGE dbo.tbl_Ref_HistoricFigure AS t
        USING (SELECT ? AS WikidataQid) AS s ON t.WikidataQid = s.WikidataQid
        WHEN MATCHED THEN UPDATE SET SiteLinkCount = ?
        WHEN NOT MATCHED THEN INSERT (WikidataQid, Name, Description, Sex, DateOfBirth, OriginalCalendar, BirthPlace,
            BirthCountry, Latitude, Longitude, DateOfDeath, AreaCode, SubCategoryCode, GeoAreaCode, SourceUrl, SiteLinkCount)
            VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?);"""
    with connect() as connection:
        cursor = connection.cursor()
        for n, p in enumerate(people, 1):
            if p.get("curated"):                               # curated rows already exist; only record their sitelink count
                cursor.execute("UPDATE dbo.tbl_Ref_HistoricFigure SET SiteLinkCount = ? WHERE WikidataQid = ?", p["links"], p["qid"])
                continue
            cursor.execute(sql, p["qid"], p["links"], p["qid"], p["name"], p["description"], p["sex"], p["dob"], p["calendar"],
                           p["place"], p["country"], p["lat"], p["lon"], p["dod"], p["area"], p["sub"], p["geo"],
                           f"https://www.wikidata.org/wiki/{p['qid']}", p["links"])
            if n % 2000 == 0:
                connection.commit()
                print(f"  {n}/{len(people)}", flush=True)
        connection.commit()
    print(f"loaded {len(people)} historic figures")


if __name__ == "__main__":
    sys.path.insert(0, str(HERE))
    {"discover": discover, "select": select, "facts": facts_stage, "build": build, "load": load}[sys.argv[1]]()
