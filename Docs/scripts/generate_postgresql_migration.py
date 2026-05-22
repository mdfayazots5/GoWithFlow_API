#!/usr/bin/env python3
from __future__ import annotations

import csv
import re
from collections import defaultdict
from pathlib import Path


ROOT = Path("/mnt/c/Live/GoWithFlow")
TMP = Path("/tmp")
OUT_DIR = ROOT / "Docs" / "PostgreSQLMigration"


BOOL_COLUMNS = set()
TABLE_PK = {}


def snake_case(name: str) -> str:
    if not name:
        return name
    s1 = re.sub(r"(.)([A-Z][a-z]+)", r"\1_\2", name)
    s2 = re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", s1)
    s2 = s2.replace("__", "_")
    return s2.lower()


def read_pipe_file(path: Path) -> list[dict[str, str]]:
    lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    meaningful = [line.rstrip() for line in lines if line.strip()]
    if len(meaningful) < 3:
        return []
    header = meaningful[0].split("|")
    rows = []
    for line in meaningful[2:]:
        if set(line.strip()) == {"-"}:
            continue
        parts = line.split("|")
        if len(parts) < len(header):
            parts += [""] * (len(header) - len(parts))
        rows.append({header[i].strip(): parts[i].strip() for i in range(len(header))})
    return rows


def parse_resultset_metadata(path: Path) -> dict[str, list[dict[str, str]]]:
    data = defaultdict(list)
    for row in read_pipe_file(path):
        data[row["procedure_name"]].append(row)
    return data


def parse_procedure_blocks(path: Path) -> dict[str, str]:
    text = path.read_text(encoding="utf-8", errors="ignore")
    blocks = {}
    for match in re.finditer(r"/\*PROC:(?P<name>[^*]+)\*/\s*(?P<body>.*?)/\*ENDPROC\*/", text, re.S):
        name = match.group("name").strip()
        body = match.group("body").rstrip()
        blocks[name] = body
    return blocks


def sql_type_to_pg(
    sql_type: str,
    max_length: str = "",
    precision: str = "",
    scale: str = "",
    identity: bool = False,
    length_is_bytes: bool = True,
) -> str:
    sql_type = sql_type.lower()
    if identity:
        if sql_type == "bigint":
            return "bigserial"
        return "serial"
    if sql_type in {"bigint"}:
        return "bigint"
    if sql_type in {"int"}:
        return "integer"
    if sql_type in {"smallint"}:
        return "smallint"
    if sql_type == "tinyint":
        return "smallint"
    if sql_type in {"bit"}:
        return "boolean"
    if sql_type in {"datetime", "datetime2", "smalldatetime"}:
        return "timestamptz"
    if sql_type == "date":
        return "date"
    if sql_type == "time":
        return "time"
    if sql_type in {"decimal", "numeric"}:
        return f"numeric({precision},{scale})" if precision and scale else "numeric"
    if sql_type == "money":
        return "numeric(19,4)"
    if sql_type in {"float"}:
        return "double precision"
    if sql_type in {"real"}:
        return "real"
    if sql_type in {"uniqueidentifier"}:
        return "uuid"
    if sql_type in {"image", "varbinary", "binary"}:
        return "bytea"
    if sql_type in {"text", "ntext"}:
        return "text"
    if sql_type in {"nvarchar", "nchar"}:
        if max_length == "-1":
            return "text"
        length = int(max_length) // 2 if length_is_bytes else int(max_length)
        return f"varchar({length})"
    if sql_type in {"varchar", "char"}:
        if max_length == "-1":
            return "text"
        return f"varchar({max_length})"
    return sql_type


def convert_default(definition: str, pg_type: str) -> str | None:
    if not definition or definition == "NULL":
        return None
    value = definition.strip()
    while value.startswith("(") and value.endswith(")"):
        value = value[1:-1].strip()
    value = value.replace("getdate()", "now()").replace("GETDATE()", "NOW()")
    value = value.replace("N'", "'")
    value = value.replace("''", "'")
    if pg_type == "boolean":
        if value in {"0", "((0))"} or value.strip("()") == "0":
            return "FALSE"
        if value in {"1", "((1))"} or value.strip("()") == "1":
            return "TRUE"
    if value.lower() == "now()":
        return "NOW()"
    if value.startswith("'") and value.endswith("'"):
        return value
    if re.fullmatch(r"-?\d+(\.\d+)?", value):
        return value
    return value


def normalize_filter(expr: str, table_columns: dict[str, dict[str, str]]) -> str:
    if not expr or expr == "NULL":
        return ""
    result = expr
    result = result.replace("[", "").replace("]", "")
    result = result.replace("=(", " = (")
    result = result.replace("<>", "!=")
    result = re.sub(r"(?<![<>=!])=(?!=)", " = ", result)
    result = re.sub(r"\s+", " ", result).strip()
    for source in sorted(table_columns.keys(), key=len, reverse=True):
        result = re.sub(rf"\b{re.escape(source)}\b", snake_case(source), result)
    for col_name, meta in table_columns.items():
        if meta["pg_type"] == "boolean":
            pg_col = snake_case(col_name)
            result = re.sub(rf"\b{pg_col}\s*=\s*0\b", f"{pg_col} = FALSE", result, flags=re.I)
            result = re.sub(rf"\b{pg_col}\s*=\s*1\b", f"{pg_col} = TRUE", result, flags=re.I)
            result = re.sub(rf"\b{pg_col}\s*=\s*\(0\)", f"{pg_col} = FALSE", result, flags=re.I)
            result = re.sub(rf"\b{pg_col}\s*=\s*\(1\)", f"{pg_col} = TRUE", result, flags=re.I)
            result = re.sub(rf"\(\s*{pg_col}\s*=\s*\(?0\)?\s*\)", f"({pg_col} = FALSE)", result, flags=re.I)
            result = re.sub(rf"\(\s*{pg_col}\s*=\s*\(?1\)?\s*\)", f"({pg_col} = TRUE)", result, flags=re.I)
    result = result.replace("))", ")")
    return result


def parse_param_block(param_block: str) -> list[dict[str, str]]:
    params = []
    for raw_line in param_block.splitlines():
        line = raw_line.strip().rstrip(",")
        if not line:
            continue
        match = re.match(r"@(?P<name>\w+)\s+(?P<type>[A-Za-z0-9_.]+(?:\([^)]+\))?)(?:\s+READONLY)?", line, re.I)
        if not match:
            continue
        raw_type = match.group("type")
        sql_type_name = raw_type.split(".")[-1]
        sql_type_base = re.sub(r"\(.*\)", "", sql_type_name)
        length_match = re.search(r"\(([^)]+)\)", sql_type_name)
        max_length = ""
        precision = ""
        scale = ""
        if length_match:
            size_bits = [part.strip() for part in length_match.group(1).split(",")]
            if len(size_bits) == 1:
                max_length = size_bits[0]
                precision = size_bits[0]
            elif len(size_bits) == 2:
                precision, scale = size_bits
        pg_type = (
            "jsonb"
            if "UtteranceTVP" in raw_type
            else sql_type_to_pg(sql_type_base, max_length, precision, scale, False, length_is_bytes=False)
        )
        params.append(
            {
                "source_name": match.group("name"),
                "name": f"p_{snake_case(match.group('name'))}",
                "sql_type": sql_type_name,
                "pg_type": pg_type,
            }
        )
    return params


def split_proc_header(proc_sql: str) -> tuple[str, list[dict[str, str]], str]:
    match = re.search(
        r"CREATE\s+PROCEDURE\s+dbo\.(?P<name>\w+)(?:\s*\((?P<params>.*?)\))?\s*AS\s*BEGIN\s*(?P<body>.*)\s*END\s*$",
        proc_sql,
        re.S | re.I,
    )
    if not match:
        raise ValueError("Unable to parse procedure definition")
    return match.group("name"), parse_param_block(match.group("params") or ""), match.group("body").strip()


def build_mappings(columns_by_table: dict[str, list[dict[str, str]]], proc_headers: dict[str, list[dict[str, str]]]) -> tuple[dict[str, str], dict[str, str], dict[str, str]]:
    table_map = {tbl: f"public.{snake_case(tbl)}" for tbl in columns_by_table}
    column_map = {}
    for rows in columns_by_table.values():
        for col in rows:
            column_map[col["column_name"]] = snake_case(col["column_name"])
    proc_map = {proc: f"public.{snake_case(proc)}" for proc in proc_headers}
    return table_map, column_map, proc_map


def collect_boolean_columns(columns_by_table: dict[str, list[dict[str, str]]]) -> set[str]:
    bools = set()
    for rows in columns_by_table.values():
        for row in rows:
            if row["pg_type"] == "boolean":
                bools.add(snake_case(row["column_name"]))
    return bools


def replace_identifiers(sql: str, table_map: dict[str, str], column_map: dict[str, str], proc_map: dict[str, str]) -> str:
    result = sql
    replacements = {}
    replacements.update(proc_map)
    replacements.update(table_map)
    replacements.update(column_map)
    for source in sorted(replacements.keys(), key=len, reverse=True):
        target = replacements[source]
        result = re.sub(rf"\b{re.escape(source)}\b", target, result)
    result = result.replace("dbo.", "public.")
    return result


def convert_exec_calls(sql: str, proc_headers: dict[str, list[dict[str, str]]]) -> str:
    pattern = re.compile(r"EXEC\s+public\.([\w\.]+)\s*(.*?);", re.S)

    def repl(match: re.Match[str]) -> str:
        proc_name = match.group(1).split(".")[-1]
        args_block = match.group(2)
        source_proc_name = next((name for name in proc_headers if snake_case(name) == proc_name), None)
        if not source_proc_name:
            return f"CALL public.{proc_name}();"
        ordered_params = proc_headers[source_proc_name]
        assignments = dict(
            (
                snake_case(name),
                value.strip().rstrip(","),
            )
            for name, value in re.findall(r"p?_(\w+)\s*=\s*([^,\n;]+)", args_block)
        )
        args = []
        for param in ordered_params:
            key = snake_case(param["source_name"])
            args.append(assignments.get(key, "NULL"))
        return f"CALL public.{snake_case(source_proc_name)}({', '.join(args)});"

    return pattern.sub(repl, sql)


def replace_named_params(sql: str, params: list[dict[str, str]], var_map: dict[str, str]) -> str:
    result = sql
    for param in params:
        result = re.sub(rf"@{re.escape(param['source_name'])}\b", param["name"], result)
    for source, target in var_map.items():
        result = re.sub(rf"@{re.escape(source)}\b", target, result)
    return result


def convert_assignment_selects(sql: str) -> str:
    pattern = re.compile(r"SELECT\s+(?P<select>.*?)\s+FROM\s", re.S | re.I)

    def repl(match: re.Match[str]) -> str:
        select_block = match.group("select")
        raw_items = [item.strip() for item in select_block.split(",") if item.strip()]
        assignments = []
        expressions = []
        for item in raw_items:
            assign_match = re.match(r"(v_\w+)\s*=\s*(.+)$", item, re.S)
            if not assign_match:
                return match.group(0)
            assignments.append(assign_match.group(1))
            expressions.append(assign_match.group(2).strip())
        return f"SELECT {', '.join(expressions)} INTO {', '.join(assignments)} FROM "

    return pattern.sub(repl, sql)


def convert_select_alias_lines(sql: str) -> str:
    converted = []
    for line in sql.splitlines():
        stripped = line.strip()
        alias_match = re.match(r"([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+?)(,?)$", stripped)
        if alias_match and not stripped.startswith(("IF ", "WHEN ", "SET ", "v_", "p_")):
            alias = snake_case(alias_match.group(1))
            expr = alias_match.group(2).strip()
            comma = alias_match.group(3)
            indent = line[: len(line) - len(line.lstrip())]
            converted.append(f"{indent}{expr} AS {alias}{comma}")
        else:
            converted.append(line)
    return "\n".join(converted)


def convert_if_begin_blocks(sql: str) -> str:
    result = sql
    pattern = re.compile(r"IF\s+(?P<cond>.*?)\s+BEGIN\s*(?P<body>.*?)\s*END;", re.S | re.I)
    previous = None
    while previous != result:
        previous = result
        result = pattern.sub(lambda m: f"IF {m.group('cond').strip()} THEN\n{m.group('body').strip()}\nEND IF;", result)
    return result


def inject_cursor_open(sql: str) -> tuple[str, bool]:
    with_matches = list(re.finditer(r"(?m)^[ \t;]*WITH\b", sql))
    if with_matches:
        pos = with_matches[-1].start()
        return sql[:pos] + "    OPEN p_result FOR\n" + sql[pos:], True
    select_matches = list(re.finditer(r"(?m)^[ \t]*SELECT\b", sql))
    if select_matches:
        pos = select_matches[-1].start()
        return sql[:pos] + "    OPEN p_result FOR\n" + sql[pos:], True
    return sql, False


def convert_tsql_body(
    proc_name: str,
    body: str,
    params: list[dict[str, str]],
    proc_headers: dict[str, list[dict[str, str]]],
    table_map: dict[str, str],
    column_map: dict[str, str],
    proc_map: dict[str, str],
    has_result: bool,
) -> tuple[str, list[str], list[str]]:
    manual_review = []
    declare_lines = []
    var_map: dict[str, str] = {}
    text = body.replace("\r\n", "\n")
    if proc_name in {"uspInsertOtpVerification", "uspVerifyOtp"}:
        manual_review.append("Backs onto removed tbl_otp_verification flow; retained as stub for manual redesign.")
        return (
            "    RAISE EXCEPTION 'Manual review required: otp verification procedures remain in SQL Server catalog but tbl_otp_verification was removed before PostgreSQL migration.';",
            manual_review,
            declare_lines,
        )

    if re.search(r"Result Set\s+2", text, re.I):
        manual_review.append("Procedure returns multiple SQL Server result sets; emitted as a stub to force explicit PostgreSQL redesign.")
        return (
            "    RAISE EXCEPTION 'Manual review required: multi-result SQL Server procedure must be redesigned for PostgreSQL or Supabase RPC consumption.';",
            sorted(set(manual_review)),
            declare_lines,
        )

    for pattern, reason in [
        (r"OPENJSON\(", "OPENJSON converted to jsonb_array_elements; validate JSON shape and counts."),
        (r"WITH\s*\(NOLOCK\)", "NOLOCK removed; review concurrency expectations."),
        (r"\bTOP\s*\(", "TOP converted heuristically; verify LIMIT semantics."),
        (r"DATEADD\(WEEK,\s*DATEDIFF\(WEEK", "SQL Server week boundary logic converted with date_trunc; verify week-start behavior."),
        (r"dbo\.UtteranceTVP|READONLY", "Table-valued parameter converted to JSONB recordset input."),
        (r"SCOPE_IDENTITY\(", "SCOPE_IDENTITY converted to lastval(); verify sequence ownership in pooled sessions."),
    ]:
        if re.search(pattern, text, re.I):
            manual_review.append(reason)

    text = re.sub(r"^\s*SET\s+NOCOUNT\s+ON;\s*$", "", text, flags=re.I | re.M)
    text = re.sub(r"^\s*SET\s+XACT_ABORT\s+ON;\s*$", "", text, flags=re.I | re.M)
    text = re.sub(r"^\s*BEGIN\s+TRAN(?:SACTION)?;\s*$", "", text, flags=re.I | re.M)
    text = re.sub(r"^\s*COMMIT\s+TRAN(?:SACTION)?;\s*$", "", text, flags=re.I | re.M)
    text = re.sub(r"^\s*ROLLBACK\s+TRAN(?:SACTION)?;\s*$", "", text, flags=re.I | re.M)
    text = re.sub(r"IF\s+@@TRANCOUNT\s*>\s*0\s*BEGIN\s*ROLLBACK\s+TRAN(?:SACTION)?;\s*END;?", "", text, flags=re.I | re.S)
    text = re.sub(r"BEGIN\s+TRY", "", text, flags=re.I)
    text = re.sub(r"END\s+TRY", "", text, flags=re.I)
    text = re.sub(r"BEGIN\s+CATCH.*?END\s+CATCH", "", text, flags=re.I | re.S)

    def declare_repl(match: re.Match[str]) -> str:
        source_name = match.group("name")
        raw_type = match.group("type")
        assignment = match.group("value")
        sql_type_base = raw_type.split(".")[-1]
        type_base = re.sub(r"\(.*\)", "", sql_type_base)
        precision = ""
        scale = ""
        max_length = ""
        length_match = re.search(r"\(([^)]+)\)", sql_type_base)
        if length_match:
            size_bits = [part.strip() for part in length_match.group(1).split(",")]
            if len(size_bits) == 1:
                max_length = size_bits[0]
                precision = size_bits[0]
            elif len(size_bits) == 2:
                precision, scale = size_bits
        pg_type = sql_type_to_pg(type_base, max_length, precision, scale, False, length_is_bytes=False)
        target_name = f"v_{snake_case(source_name)}"
        var_map[source_name] = target_name
        value_sql = ""
        if assignment:
            value_sql = f" := {assignment.strip()}"
        declare_lines.append(f"    {target_name} {pg_type}{value_sql};")
        return ""

    text = re.sub(
        r"^\s*DECLARE\s+@(?P<name>\w+)\s+(?P<type>[A-Za-z0-9_.]+(?:\([^)]+\))?)(?:\s*=\s*(?P<value>.*?))?;\s*$",
        declare_repl,
        text,
        flags=re.I | re.M,
    )

    text = replace_named_params(text, params, var_map)
    text = text.replace("N'", "'")
    text = text.replace(";THROW;", "RAISE;")
    text = re.sub(r"THROW\s+\d+\s*,\s*'([^']*)'\s*,\s*\d+\s*;", lambda m: f"RAISE EXCEPTION '{m.group(1)}';", text, flags=re.I)
    text = re.sub(r"\bTHROW\s*;", "RAISE;", text, flags=re.I)
    text = re.sub(r"SCOPE_IDENTITY\(\)", "lastval()", text, flags=re.I)
    text = re.sub(r"ISNULL\s*\(", "COALESCE(", text, flags=re.I)
    text = re.sub(r"GETDATE\s*\(\s*\)", "NOW()", text, flags=re.I)
    text = re.sub(
        r"DATEFROMPARTS\s*\(\s*YEAR\s*\(\s*NOW\(\)\s*\)\s*,\s*MONTH\s*\(\s*NOW\(\)\s*\)\s*,\s*1\s*\)",
        "make_date(EXTRACT(YEAR FROM NOW())::integer, EXTRACT(MONTH FROM NOW())::integer, 1)",
        text,
        flags=re.I,
    )
    text = re.sub(r"CAST\s*\(\s*NOW\(\)\s+AS\s+DATE\s*\)", "CURRENT_DATE", text, flags=re.I)
    text = re.sub(r"DATEADD\s*\(\s*DAY\s*,\s*(-?\d+)\s*,\s*CURRENT_DATE\s*\)", lambda m: f"(CURRENT_DATE + INTERVAL '{m.group(1)} day')", text, flags=re.I)
    text = re.sub(r"DATEADD\s*\(\s*WEEK\s*,\s*(-?\d+)\s*,\s*CURRENT_DATE\s*\)", lambda m: f"(CURRENT_DATE + INTERVAL '{m.group(1)} week')", text, flags=re.I)
    text = re.sub(r"DATEADD\s*\(\s*DAY\s*,\s*(-?\d+)\s*,\s*NOW\(\)\s*\)", lambda m: f"(NOW() + INTERVAL '{m.group(1)} day')", text, flags=re.I)
    text = re.sub(r"DATEADD\s*\(\s*WEEK\s*,\s*(-?\d+)\s*,\s*NOW\(\)\s*\)", lambda m: f"(NOW() + INTERVAL '{m.group(1)} week')", text, flags=re.I)
    text = re.sub(r"DATEADD\s*\(\s*MINUTE\s*,\s*([^,]+?)\s*,\s*NOW\(\)\s*\)", r"(NOW() + make_interval(mins => \1::integer))", text, flags=re.I)
    text = re.sub(
        r"DATEADD\s*\(\s*WEEK\s*,\s*DATEDIFF\s*\(\s*WEEK\s*,\s*0\s*,\s*(.*?)\)\s*,\s*0\s*\)",
        r"date_trunc('week', \1)",
        text,
        flags=re.I | re.S,
    )
    text = re.sub(
        r"DATEDIFF\s*\(\s*SECOND\s*,\s*([^,]+?)\s*,\s*NOW\(\)\s*\)",
        r"EXTRACT(EPOCH FROM (NOW() - \1))::integer",
        text,
        flags=re.I,
    )
    text = re.sub(r"CONVERT\s*\(\s*NVARCHAR\s*\(\s*16\s*\)\s*,\s*([^,]+?)\s*,\s*23\s*\)", r"TO_CHAR(\1, 'YYYY-MM-DD')", text, flags=re.I)
    text = re.sub(r"WITH\s*\(NOLOCK\)", "", text, flags=re.I)
    text = re.sub(
        r"FROM\s+OPENJSON\s*\(\s*COALESCE\(([^,]+?),\s*'?\[\]'?\)\s*\)",
        r"FROM jsonb_array_elements(COALESCE(\1, '[]')::jsonb) AS json_value",
        text,
        flags=re.I,
    )
    text = re.sub(r"CAST\s*\(\s*(.*?)\s+AS\s+DECIMAL\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)\s*\)", r"CAST(\1 AS numeric(\2,\3))", text, flags=re.I | re.S)
    text = re.sub(r"CAST\s*\(\s*(.*?)\s+AS\s+BIT\s*\)", r"CAST(\1 AS boolean)", text, flags=re.I | re.S)
    text = re.sub(r"SELECT\s+TOP\s*\(\s*([^)]+)\s*\)", r"SELECT /* MANUAL_REVIEW_TOP:\1 */", text, flags=re.I)
    text = replace_identifiers(text, table_map, column_map, proc_map)
    text = re.sub(r"public\.public\.", "public.", text)
    text = convert_assignment_selects(text)
    text = convert_select_alias_lines(text)
    text = re.sub(
        r"IF\s+OBJECT_ID\(\s*'public\.([a-z_]+)'\s*,\s*'U'\s*\)\s+IS\s+NOT\s+NULL",
        r"IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '\1')",
        text,
        flags=re.I,
    )
    text = convert_if_begin_blocks(text)
    text = convert_exec_calls(text, proc_headers)
    text = re.sub(r"public\.public\.", "public.", text)

    for bool_col in BOOL_COLUMNS:
        text = re.sub(rf"\b{bool_col}\s*=\s*0\b", f"{bool_col} = FALSE", text, flags=re.I)
        text = re.sub(rf"\b{bool_col}\s*=\s*1\b", f"{bool_col} = TRUE", text, flags=re.I)
        text = re.sub(rf"COALESCE\(\s*([^,]+?\.{bool_col}|{bool_col})\s*,\s*0\s*\)", r"COALESCE(\1, FALSE)", text, flags=re.I)
        text = re.sub(rf"COALESCE\(\s*([^,]+?\.{bool_col}|{bool_col})\s*,\s*1\s*\)", r"COALESCE(\1, TRUE)", text, flags=re.I)

    if "p_utterances" in {param["name"] for param in params}:
        text = text.replace(
            "FROM p_utterances AS utv",
            "FROM jsonb_to_recordset(p_utterances) AS utv(sequence_id integer, speaker_label varchar(128), english_text varchar(1024), hint_text varchar(1024), grammar_tag varchar(128), context_tag varchar(128), focus_word varchar(128), pronunciation_note varchar(512))",
        )

    if has_result:
        text, injected = inject_cursor_open(text)
        if not injected:
            manual_review.append("No result query could be identified for refcursor opening; verify PostgreSQL return behavior.")

    text = "\n".join(line.rstrip() for line in text.splitlines() if line.strip())
    return text, sorted(set(manual_review)), declare_lines


def render_header(filename: str, description: str, order_no: int, dependencies: str, counts: dict[str, int], incompatibilities: str, manual_review: str) -> str:
    return f"""-- ============================================
-- File: {filename}
-- Description: {description}
-- Run order: {order_no} of 10
-- Dependencies: {dependencies}
-- ============================================
-- Tables migrated: {counts['tables']}
-- Views migrated: {counts['views']}
-- Functions migrated: {counts['functions']}
-- Stored Procedures migrated: {counts['procedures']}
-- Triggers migrated: {counts['triggers']}
-- Indexes migrated: {counts['indexes']}
-- Known incompatibilities: {incompatibilities}
-- Manual review required: {manual_review}
"""


def write_file(filename: str, body: str) -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / filename).write_text(body.rstrip() + "\n", encoding="utf-8")


def main() -> None:
    columns_rows = read_pipe_file(TMP / "gwf_columns.txt")
    keys_rows = read_pipe_file(TMP / "gwf_key_constraints.txt")
    fk_rows = read_pipe_file(TMP / "gwf_foreign_keys.txt")
    index_rows = read_pipe_file(TMP / "gwf_indexes.txt")
    tvp_rows = read_pipe_file(TMP / "gwf_table_types.txt")
    proc_resultsets = parse_resultset_metadata(TMP / "gwf_proc_resultsets.txt")
    proc_blocks = parse_procedure_blocks(TMP / "gwf_sqlserver_procedures.sql")

    columns_by_table: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in columns_rows:
        row["pg_table"] = snake_case(row["table_name"])
        row["pg_column"] = snake_case(row["column_name"])
        row["pg_type"] = sql_type_to_pg(
            row["data_type"],
            row["max_length"],
            row["precision"],
            row["scale"],
            row["is_identity"] == "1",
        )
        columns_by_table[row["table_name"]].append(row)
        if row["pg_type"] == "boolean":
            BOOL_COLUMNS.add(row["pg_column"])

    unique_constraints_by_table: dict[str, list[dict[str, str]]] = defaultdict(list)
    pk_by_table: dict[str, dict[str, str]] = {}
    for row in keys_rows:
        if row["type_desc"] == "PRIMARY_KEY_CONSTRAINT":
            pk_by_table[row["table_name"]] = row
            TABLE_PK[row["table_name"]] = row["column_names"].split(",")[0]
        elif row["type_desc"] == "UNIQUE_CONSTRAINT":
            unique_constraints_by_table[row["table_name"]].append(row)

    proc_headers: dict[str, list[dict[str, str]]] = {}
    for proc_name, proc_sql in proc_blocks.items():
        _, params, _ = split_proc_header(proc_sql)
        proc_headers[proc_name] = params

    table_map, column_map, proc_map = build_mappings(columns_by_table, proc_headers)

    counts = {
        "tables": len(columns_by_table),
        "views": 0,
        "functions": 0,
        "procedures": len(proc_blocks),
        "triggers": 0,
        "indexes": len(index_rows),
    }

    file1 = render_header(
        "01_extensions.sql",
        "Enable PostgreSQL extensions required by the migrated schema and Supabase integration layer.",
        1,
        "NONE",
        counts,
        "NONE",
        "NONE",
    )
    file1 += "\nBEGIN;\n\nCREATE EXTENSION IF NOT EXISTS pgcrypto;\n\nCOMMIT;\n"
    write_file("01_extensions.sql", file1)

    schema_lines = [
        render_header(
            "02_schema.sql",
            "Create base PostgreSQL tables converted from the live SQL Server schema without foreign keys.",
            2,
            "01_extensions.sql",
            counts,
            "__ef_migrations_history omitted because Supabase deployment should not carry SQL Server EF bookkeeping.",
            "Review default timezone semantics on timestamptz columns if the source application assumed SQL Server local server time.",
        ),
        "\nBEGIN;\n",
    ]
    for table_name in sorted(columns_by_table):
        rows = columns_by_table[table_name]
        pg_table = snake_case(table_name)
        schema_lines.append(f"CREATE TABLE IF NOT EXISTS public.{pg_table} (")
        col_defs = []
        for row in rows:
            pieces = [f"    {row['pg_column']} {row['pg_type']}"]
            if row["is_identity"] == "1":
                pass
            if row["is_nullable"] == "0":
                pieces.append("NOT NULL")
            default_sql = convert_default(row["default_definition"], row["pg_type"])
            if default_sql:
                pieces.append(f"DEFAULT {default_sql}")
            if row["check_definition"] and row["check_definition"] != "NULL":
                pieces.append(f"CHECK ({normalize_filter(row['check_definition'], {r['column_name']: r for r in rows})})")
            col_defs.append(" ".join(pieces))
        pk = pk_by_table.get(table_name)
        if pk:
            pk_cols = ", ".join(snake_case(col) for col in pk["column_names"].split(","))
            col_defs.append(f"    CONSTRAINT {snake_case(pk['constraint_name'])} PRIMARY KEY ({pk_cols})")
        for uq in unique_constraints_by_table.get(table_name, []):
            uq_cols = ", ".join(snake_case(col) for col in uq["column_names"].split(","))
            col_defs.append(f"    CONSTRAINT {snake_case(uq['constraint_name'])} UNIQUE ({uq_cols})")
        schema_lines.append(",\n".join(col_defs))
        schema_lines.append(");\n")
    schema_lines.append("COMMIT;\n")
    write_file("02_schema.sql", "\n".join(schema_lines))

    index_lines = [
        render_header(
            "03_constraints_indexes.sql",
            "Add foreign keys and secondary indexes converted from the live SQL Server catalog.",
            3,
            "01_extensions.sql, 02_schema.sql",
            counts,
            "Filtered indexes translated to partial indexes; verify predicate selectivity under PostgreSQL planner.",
            "Composite unique indexes from SQL Server filtered indexes need workload validation after migration.",
        ),
        "\nBEGIN;\n",
    ]
    for row in fk_rows:
        parent_table = snake_case(row["parent_table"])
        parent_cols = ", ".join(snake_case(c) for c in row["parent_columns"].split(","))
        ref_table = snake_case(row["ref_table"])
        ref_cols = ", ".join(snake_case(c) for c in row["ref_columns"].split(","))
        delete_action = "" if row["delete_referential_action_desc"] == "NO_ACTION" else f" ON DELETE {row['delete_referential_action_desc'].replace('_', ' ')}"
        update_action = "" if row["update_referential_action_desc"] == "NO_ACTION" else f" ON UPDATE {row['update_referential_action_desc'].replace('_', ' ')}"
        index_lines.append(
            f"ALTER TABLE public.{parent_table} ADD CONSTRAINT {snake_case(row['fk_name'])} FOREIGN KEY ({parent_cols}) "
            f"REFERENCES public.{ref_table} ({ref_cols}){delete_action}{update_action};"
        )
    index_lines.append("")
    for row in index_rows:
        table = snake_case(row["table_name"])
        cols = ", ".join(snake_case(c) for c in row["key_columns"].split(",") if c and c != "NULL")
        included = ""
        if row["included_columns"] and row["included_columns"] != "NULL":
            included_cols = ", ".join(snake_case(c) for c in row["included_columns"].split(","))
            included = f" INCLUDE ({included_cols})"
        unique = "UNIQUE " if row["is_unique"] == "1" else ""
        where_clause = ""
        if row["has_filter"] == "1" and row["filter_definition"] and row["filter_definition"] != "NULL":
            table_columns = {r["column_name"]: r for r in columns_by_table[row["table_name"]]}
            where_clause = f" WHERE {normalize_filter(row['filter_definition'], table_columns)}"
        index_lines.append(
            f"CREATE {unique}INDEX IF NOT EXISTS {snake_case(row['index_name'])} ON public.{table} ({cols}){included}{where_clause};"
        )
    index_lines.append("\nCOMMIT;\n")
    write_file("03_constraints_indexes.sql", "\n".join(index_lines))

    file4 = render_header(
        "04_views.sql",
        "Create PostgreSQL views converted from SQL Server views.",
        4,
        "01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql",
        counts,
        "NONE",
        "NONE",
    )
    file4 += "\nBEGIN;\n\n-- No user-defined SQL Server views exist in GoWithFlowDB.\n\nCOMMIT;\n"
    write_file("04_views.sql", file4)

    function_incompat = "NONE"
    function_review = "NONE"
    if tvp_rows:
        function_incompat = "SQL Server user-defined table type dbo.UtteranceTVP has no direct PostgreSQL equivalent and is consumed as JSONB by the migrated bulk-load procedure."
        function_review = "Validate JSON payload contract used in public.usp_bulk_insert_utterance against the original dbo.UtteranceTVP column order."
    file5 = render_header(
        "05_functions.sql",
        "Create PostgreSQL functions converted from SQL Server scalar and table-valued functions.",
        5,
        "01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql",
        counts,
        function_incompat,
        function_review,
    )
    file5 += "\nBEGIN;\n\n-- No user-defined SQL Server scalar or table-valued functions exist in GoWithFlowDB.\n\nCOMMIT;\n"
    write_file("05_functions.sql", file5)

    proc_lines = [
        render_header(
            "06_stored_procedures.sql",
            "Create PostgreSQL procedure entry points corresponding to the live SQL Server stored procedure catalog.",
            6,
            "01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql, 05_functions.sql",
            counts,
            "Automated procedural translation is unsafe for this catalog because it contains SQL Server-specific rowset procedures, TVPs, OPENJSON analytics, TOP queries, local transaction control, and residual OTP objects after table removal.",
            "Every procedure body is emitted as an executable PostgreSQL stub that raises a manual-review exception until the SQL Server logic is rewritten and validated.",
        ),
        "\nBEGIN;\n",
    ]

    for proc_name in sorted(proc_blocks):
        _, params, body = split_proc_header(proc_blocks[proc_name])
        has_result = any(row.get("column_ordinal") not in {"", "NULL", None} for row in proc_resultsets.get(proc_name, []))
        manual_review = []
        for pattern, reason in [
            (r"OPENJSON\(", "Uses OPENJSON and needs JSONB/recordset rewrites."),
            (r"WITH\s*\(NOLOCK\)", "Uses NOLOCK hints that have no PostgreSQL equivalent."),
            (r"\bTOP\s*\(", "Uses TOP and needs LIMIT/FETCH rewrites."),
            (r"SCOPE_IDENTITY\(", "Uses SCOPE_IDENTITY and needs RETURNING/lastval review."),
            (r"READONLY|UtteranceTVP", "Uses a SQL Server table-valued parameter and needs a PostgreSQL JSONB/composite input redesign."),
            (r"BEGIN\s+TRAN|COMMIT\s+TRAN|ROLLBACK\s+TRAN", "Uses explicit SQL Server transaction control inside the procedure body."),
            (r"THROW|RAISERROR", "Uses SQL Server exception syntax that must be rewritten for PL/pgSQL."),
            (r"DATEADD|DATEDIFF|DATEFROMPARTS|CONVERT\(", "Uses SQL Server date/time conversion functions that need PostgreSQL rewrites."),
        ]:
            if re.search(pattern, body, re.I):
                manual_review.append(reason)
        if proc_name in {"uspInsertOtpVerification", "uspVerifyOtp"}:
            manual_review.append("References the removed tblOtpVerification flow and must be redesigned before PostgreSQL deployment.")
        if re.search(r"Result Set\s+2", body, re.I):
            manual_review.append("Returns multiple SQL Server result sets and needs a PostgreSQL cursor or JSON contract redesign.")
        if not manual_review:
            manual_review.append("Logic rewrite required from T-SQL to PL/pgSQL before production use.")
        param_defs = [f"IN {param['name']} {param['pg_type']}" for param in params]
        if has_result:
            param_defs.append(f"INOUT p_result refcursor DEFAULT '{snake_case(proc_name)}_result'")
        proc_lines.append(f"-- Procedure: public.{snake_case(proc_name)}")
        for note in manual_review:
            proc_lines.append(f"-- [MANUAL REVIEW REQUIRED: {note}]")
        proc_lines.append(f"CREATE OR REPLACE PROCEDURE public.{snake_case(proc_name)}({', '.join(param_defs)})")
        proc_lines.append("LANGUAGE plpgsql")
        proc_lines.append("AS $$")
        proc_lines.append("BEGIN")
        if has_result:
            proc_lines.append(
                f"    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure {proc_name} and return rows through refcursor p_result before enabling PostgreSQL clients.';"
            )
        else:
            proc_lines.append(
                f"    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure {proc_name} to validated PL/pgSQL before production use.';"
            )
        proc_lines.append("EXCEPTION WHEN OTHERS THEN")
        proc_lines.append("    RAISE;")
        proc_lines.append("END;")
        proc_lines.append("$$;\n")
    proc_lines.append("COMMIT;\n")
    write_file("06_stored_procedures.sql", "\n".join(proc_lines))

    file7 = render_header(
        "07_triggers.sql",
        "Create PostgreSQL triggers converted from SQL Server triggers.",
        7,
        "01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql, 05_functions.sql, 06_stored_procedures.sql",
        counts,
        "NONE",
        "NONE",
    )
    file7 += "\nBEGIN;\n\n-- No user-defined SQL Server table triggers exist in GoWithFlowDB.\n\nCOMMIT;\n"
    write_file("07_triggers.sql", file7)

    file8 = render_header(
        "08_seed_data.sql",
        "Insert lookup and reference data required after schema creation.",
        8,
        "01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql, 05_functions.sql, 06_stored_procedures.sql, 07_triggers.sql",
        counts,
        "Current live database contains no dedicated lookup/reference tables; application rows such as sessions, scripts, and users are intentionally excluded from seed scripts.",
        "NONE",
    )
    file8 += "\nBEGIN;\n\n-- No reference or lookup seed data was detected in the live SQL Server schema.\n\nCOMMIT;\n"
    write_file("08_seed_data.sql", file8)

    seq_lines = [
        render_header(
            "09_sequences_reset.sql",
            "Reset PostgreSQL serial sequences after data loads.",
            9,
            "01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql, 05_functions.sql, 06_stored_procedures.sql, 07_triggers.sql, 08_seed_data.sql",
            counts,
            "NONE",
            "Run after bulk data loads; empty tables intentionally reset to 1 via COALESCE.",
        ),
        "\nBEGIN;\n",
    ]
    for table_name in sorted(columns_by_table):
        for row in columns_by_table[table_name]:
            if row["is_identity"] == "1":
                pg_table = snake_case(table_name)
                pg_col = row["pg_column"]
                seq_lines.append(
                    f"SELECT setval(pg_get_serial_sequence('public.{pg_table}', '{pg_col}'), COALESCE((SELECT MAX({pg_col}) FROM public.{pg_table}), 1), TRUE);"
                )
    seq_lines.append("\nCOMMIT;\n")
    write_file("09_sequences_reset.sql", "\n".join(seq_lines))

    rls_lines = [
        render_header(
            "10_rls_policies.sql",
            "Enable Supabase Row Level Security and map legacy SQL Server user roles to Supabase auth identities.",
            10,
            "01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql, 05_functions.sql, 06_stored_procedures.sql, 07_triggers.sql, 08_seed_data.sql, 09_sequences_reset.sql",
            counts,
            "Legacy SQL Server authentication used bigint user ids and application-issued JWTs. Supabase auth.uid() needs an explicit bridge to mapped app users.",
            "Backfill public.user_auth_map after importing users and before exposing authenticated client access.",
        ),
        "\nBEGIN;\n",
        "CREATE TABLE IF NOT EXISTS public.user_auth_map (",
        "    auth_user_id uuid PRIMARY KEY REFERENCES auth.users (id) ON DELETE CASCADE,",
        "    user_id bigint NOT NULL UNIQUE REFERENCES public.tbl_user (user_id) ON DELETE CASCADE,",
        "    created_at timestamptz NOT NULL DEFAULT NOW()",
        ");\n",
        "ALTER TABLE public.user_auth_map ENABLE ROW LEVEL SECURITY;\n",
        "CREATE OR REPLACE FUNCTION public.current_app_user_id()",
        "RETURNS bigint",
        "LANGUAGE sql",
        "STABLE",
        "AS $$",
        "    SELECT uam.user_id",
        "    FROM public.user_auth_map AS uam",
        "    WHERE uam.auth_user_id = auth.uid()",
        "    LIMIT 1;",
        "$$;\n",
        "CREATE OR REPLACE FUNCTION public.current_app_is_admin()",
        "RETURNS boolean",
        "LANGUAGE sql",
        "STABLE",
        "AS $$",
        "    SELECT EXISTS (",
        "        SELECT 1",
        "        FROM public.user_auth_map AS uam",
        "        JOIN public.tbl_user AS usr ON usr.user_id = uam.user_id",
        "        WHERE uam.auth_user_id = auth.uid()",
        "          AND usr.role = 'ADMIN'",
        "          AND usr.is_deleted = FALSE",
        "    );",
        "$$;\n",
        "DROP POLICY IF EXISTS user_auth_map_self_select ON public.user_auth_map;",
        "CREATE POLICY user_auth_map_self_select ON public.user_auth_map",
        "    FOR SELECT",
        "    USING (auth.uid() = auth_user_id);\n",
        "DROP POLICY IF EXISTS user_auth_map_service_manage ON public.user_auth_map;",
        "CREATE POLICY user_auth_map_service_manage ON public.user_auth_map",
        "    FOR ALL",
        "    USING (auth.role() = 'service_role')",
        "    WITH CHECK (auth.role() = 'service_role');\n",
    ]

    owned_tables = {
        "tbl_user": "user_id",
        "tbl_refresh_token": "user_id",
        "tbl_user_badge": "user_id",
        "tbl_user_streak": "user_id",
        "tbl_voice_analysis": "user_id",
        "tbl_mistake": "user_id",
        "tbl_repractice_session": "user_id",
        "tbl_session_member": "user_id",
    }
    admin_tables = {
        "tbl_admin_note",
        "tbl_dashboard_metric",
        "tbl_script",
        "tbl_script_version",
        "tbl_utterance",
        "tbl_session",
        "tbl_turn_state",
        "tbl_listener_feedback",
        "tbl_repractice_utterance",
    }

    for table_name in sorted(snake_case(t) for t in columns_by_table):
        rls_lines.append(f"ALTER TABLE public.{table_name} ENABLE ROW LEVEL SECURITY;")
        if table_name in owned_tables:
            owner_col = owned_tables[table_name]
            rls_lines.append(f"DROP POLICY IF EXISTS {table_name}_owner_select ON public.{table_name};")
            rls_lines.append(
                f"CREATE POLICY {table_name}_owner_select ON public.{table_name} "
                f"FOR SELECT USING ({owner_col} = public.current_app_user_id() OR public.current_app_is_admin());"
            )
            rls_lines.append(f"DROP POLICY IF EXISTS {table_name}_owner_modify ON public.{table_name};")
            rls_lines.append(
                f"CREATE POLICY {table_name}_owner_modify ON public.{table_name} "
                f"FOR ALL USING ({owner_col} = public.current_app_user_id() OR public.current_app_is_admin()) "
                f"WITH CHECK ({owner_col} = public.current_app_user_id() OR public.current_app_is_admin());"
            )
        elif table_name in admin_tables:
            rls_lines.append(f"DROP POLICY IF EXISTS {table_name}_admin_only ON public.{table_name};")
            rls_lines.append(
                f"CREATE POLICY {table_name}_admin_only ON public.{table_name} "
                f"FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') "
                f"WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');"
            )
        else:
            rls_lines.append(f"-- [MANUAL REVIEW REQUIRED: no ownership policy template was inferred for public.{table_name}; service_role access only until reviewed.]")
            rls_lines.append(f"DROP POLICY IF EXISTS {table_name}_service_role_only ON public.{table_name};")
            rls_lines.append(
                f"CREATE POLICY {table_name}_service_role_only ON public.{table_name} "
                f"FOR ALL USING (auth.role() = 'service_role') WITH CHECK (auth.role() = 'service_role');"
            )
        rls_lines.append("")
    rls_lines.append("COMMIT;\n")
    write_file("10_rls_policies.sql", "\n".join(rls_lines))


if __name__ == "__main__":
    main()
