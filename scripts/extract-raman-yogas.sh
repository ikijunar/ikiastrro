#!/usr/bin/env bash
set -euo pipefail

source_djvu="/mnt/d/Vedic Astrology/Vedic Astology Books/B. V. Raman/300 Important Combinations.djvu"
output_dir="/mnt/d/@ClaudeSpace/BookExtracts"
work_dir="$output_dir/_work/300-important-combinations_p1-352"
pages_dir="$work_dir/pages"
ocr_dir="$work_dir/ocr"
body_path="$work_dir/body.md"
progress_path="$work_dir/progress.log"
draft_path="$output_dir/300-important-combinations_p1-352_draft.md"

mkdir -p "$pages_dir" "$ocr_dir"
: > "$body_path"
: > "$progress_path"

for page in $(seq 1 352); do
    timestamp=$(date -Iseconds)
    tiff_path="$pages_dir/page$page.tiff"
    ocr_base="$ocr_dir/page$page"

    if timeout 60 ddjvu -format=tiff -page="$page" "$source_djvu" "$tiff_path" >/dev/null 2>&1 \
        && timeout 60 tesseract "$tiff_path" "$ocr_base" >/dev/null 2>&1 \
        && test -f "$ocr_base.txt"; then
        printf '<!-- PAGE %s (source djvu page order) -->\n\n' "$page" >> "$body_path"
        sed -E ':a;N;$!ba;s/([[:lower:]])-\r?\n([[:lower:]])/\1\2/g' "$ocr_base.txt" >> "$body_path"
        printf '\n\n' >> "$body_path"
        printf '%s PAGE %s OK\n' "$timestamp" "$page" >> "$progress_path"
    else
        printf '%s PAGE %s FAILED\n' "$timestamp" "$page" >> "$progress_path"
    fi

    if ((page % 25 == 0)); then
        printf 'PROGRESS %s/352\n' "$page"
    fi
done

{
    printf '%s\n' \
        '---' \
        'title: 300 Important Combinations' \
        'author: B. V. Raman' \
        'publisher: TODO' \
        'edition: TODO' \
        'isbn: TODO' \
        'source_file: D:\Vedic Astrology\Vedic Astology Books\B. V. Raman\300 Important Combinations.djvu' \
        'source_format: DjVu rendered via Ubuntu ddjvu and OCR with Tesseract 5' \
        'pages_extracted: 1-352 (djvu page order)' \
        'conversion_method: cproj_book_to_md workflow (ddjvu -> TIFF -> Tesseract OCR -> conservative cleanup)' \
        'conversion_date: 2026-09-08' \
        'review_status: AUTOMATED DRAFT - not manually verified; yoga predicates and citations require page-image checking before implementation' \
        '---' \
        ''
    cat "$body_path"
} > "$draft_path"

printf 'DRAFT %s\n' "$draft_path"
