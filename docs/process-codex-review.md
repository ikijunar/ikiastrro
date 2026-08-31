# External reviews (Codex / ChatGPT)

Claude Code is the **build engine** for this project. Codex CLI (signed in with a
free ChatGPT account) is used only as an **outside reviewer**.

## How it works

1. Run a review:
   ```powershell
   .\scripts\codex-review.ps1                                  # -> codex-review.md
   .\scripts\codex-review.ps1 -Focus "nakshatra boundary rounding and ayanamsa"
   .\scripts\codex-review.ps1 -Paths src,db                    # smaller bundle, saves quota
   .\scripts\codex-review.ps1 -Versioned                       # keep a timestamped copy
   ```
2. The script bundles source + SQL + design docs into one prompt and pipes it to
   `codex exec`. Codex **never touches the filesystem** - it only reads the piped
   text and replies. (On native Windows the Codex sandbox blocks all shell
   commands anyway, so bundling is also the only thing that works here. Use WSL2
   if you later want interactive repo exploration.)
3. Output is written to **`D:\@ChatGPT\vedic_horo_gen\`** (the global default
   ChatGPT folder, per `~/.claude/CLAUDE.md`):
   - no `-Focus`  -> `codex-review.md`
   - with `-Focus "x y"` -> `codex-review-x-y.md`
   - the file is **overwritten** each run; pass `-Versioned` to append a
     `-yyyy-MM-dd-HHmm` stamp and keep history instead.
4. In Claude Code, ask it to triage the file: accept / reject / defer each point,
   then implement the accepted ones.

## Guardrails

- Codex gets a read-only copy of the text, no write path. Only Claude edits code.
- Run reviews at checkpoints (feature done, before a refactor), not continuously -
  keeps usage under the free-tier limit.
- Review outputs live in `D:\@ChatGPT\` (outside the repo), so they are not part
  of the git history - re-run to refresh, or use `-Versioned` for a keepsake.

## Auth & account

Full details — account, CLI version, free-plan limits, the native-Windows sandbox
limitation, and the API-key fallback — live in `D:\@ClaudeSpace\@RefSpace\my_chatgpt.md`.

Quick version: `codex login` (browser, free ChatGPT account), check with
`codex login status`. The account default model is used — named models like
`gpt-5.x-codex` are rejected on a ChatGPT sign-in, so `-Model` only works after
switching to an API key.
