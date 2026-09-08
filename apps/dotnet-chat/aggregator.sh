#!/bin/bash

dirs=(".")
output="result.txt"

# очищаем файл
> "$output"

current_dir=$(pwd)

# фикс UTF-8 окружения (LEVEL 1 FIX)
export LANG=C.UTF-8
export LC_ALL=C.UTF-8

for dir in "${dirs[@]}"; do
    find "$dir" -type f \( -name "*.cs" -o -name "*.razor" -o -name "*.css" \) -not -path "*/bin/*" -not -path "*/obj/*" -not -path "*/lib/*" -not -path "*/bootstrap/*" | sort | while read -r file; do

        relative_path=$(realpath --relative-to="$current_dir" "$file")

        echo "//${relative_path}:" >> "$output"
        echo "//----------------------------------------" >> "$output"

        # LEVEL 2 FIX: удаляем BOM + нормализуем UTF-8
        iconv -f utf-8 -t utf-8 "$file" \
            | sed '1s/^\xEF\xBB\xBF//' \
            | sed '/^[[:space:]]*$/d' \
            >> "$output"

        echo "" >> "$output"
    done
done

# LEVEL 3 FIX: финальная нормализация файла
iconv -f utf-8 -t utf-8 "$output" -o "$output"