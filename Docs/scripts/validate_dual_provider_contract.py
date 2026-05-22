#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BACKEND_ROOT = ROOT / "Backend"
INFRA_ROOT = BACKEND_ROOT / "GoWithFlow.Infrastructure"
PG_MIGRATION_ROOT = BACKEND_ROOT / "Docs" / "PostgreSQLMigration"
DB_COMMAND_HELPER = INFRA_ROOT / "Data" / "DbCommandHelper.cs"

TABLE_REQUIRED_ROUTINES = {
    "uspgetalluserbysearch",
    "uspexportuserreportdata",
    "uspgetmistakebyuseridwithfilter",
    "uspgetrepracticesessionbyrepracticesessionid",
    "uspgetrepracticesessionlistbyuserid",
    "uspgetscriptbysearch",
    "uspgetscriptdetailbyscriptid",
    "uspgetsessionbyjoincode",
    "uspgetsessionbysessionid",
    "uspgetsessioncompletionsummary",
    "uspgetsessiondetailbysessionid",
    "uspgetsessionlistbyuserid",
    "uspgetstreakdatabyuserid",
    "uspgetuserdashboardsummarybyuserid",
    "uspgetuserdetailbyuserid",
    "uspgetuserfullreportbyuserid",
    "uspgetuserreportsummarylist",
}

PASSWORDHASH_512_ROUTINES = {
    "uspgetuserbymobilenumber",
    "uspgetuserbyuserid",
    "uspgetuserdetailbyuserid",
}

FUNCTION_PATTERN = re.compile(
    r"CREATE\s+OR\s+REPLACE\s+FUNCTION\s+(?:public\.)?(?P<name>[a-zA-Z0-9_]+)\s*"
    r"\((?P<params>.*?)\)\s*RETURNS\s+(?P<returns>.*?)\s+AS\s+\$\$",
    re.IGNORECASE | re.DOTALL,
)


@dataclass(frozen=True)
class FunctionDefinition:
    name: str
    returns: str
    path: Path
    line_number: int


def sql_server_to_postgres_name(routine_name: str) -> str:
    return routine_name.split(".")[-1].lower()


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def collect_code_routines() -> set[str]:
    routines: set[str] = set()
    pattern = re.compile(r'"(dbo\.usp[A-Za-z0-9]+)"')

    for path in INFRA_ROOT.rglob("*.cs"):
        if any(part in {"bin", "obj"} for part in path.parts):
            continue
        text = read_text(path)
        routines.update(match.group(1) for match in pattern.finditer(text))

    return routines


def normalize_return_clause(return_clause: str) -> str:
    return " ".join(return_clause.split())


def collect_latest_postgres_function_definitions() -> dict[str, FunctionDefinition]:
    definitions: dict[str, FunctionDefinition] = {}

    for path in sorted(PG_MIGRATION_ROOT.glob("*.sql")):
        text = read_text(path)
        for match in FUNCTION_PATTERN.finditer(text):
            definitions[match.group("name").lower()] = FunctionDefinition(
                name=match.group("name").lower(),
                returns=normalize_return_clause(match.group("returns")),
                path=path,
                line_number=text[: match.start()].count("\n") + 1,
            )

    return definitions


def is_refcursor_return(return_clause: str) -> bool:
    return bool(re.search(r"\bREFCURSOR\b", return_clause, re.IGNORECASE))


def is_table_return(return_clause: str) -> bool:
    normalized = return_clause.upper()
    return normalized.startswith("TABLE(") or normalized.startswith("TABLE (")


def validate_routine_mapping() -> list[str]:
    errors: list[str] = []
    code_routines = collect_code_routines()
    postgres_functions = collect_latest_postgres_function_definitions()

    missing = sorted(
        f"{routine} -> public.{sql_server_to_postgres_name(routine)}"
        for routine in code_routines
        if sql_server_to_postgres_name(routine) not in postgres_functions
    )

    if missing:
        errors.append("Missing PostgreSQL functions for code-called SQL Server routines:")
        errors.extend(f"  - {entry}" for entry in missing)

    return errors


def validate_active_postgres_contracts() -> list[str]:
    errors: list[str] = []
    code_routines = collect_code_routines()
    latest_definitions = collect_latest_postgres_function_definitions()

    for routine in sorted(code_routines):
        postgres_name = sql_server_to_postgres_name(routine)
        definition = latest_definitions.get(postgres_name)
        if definition is None:
            continue

        if is_refcursor_return(definition.returns):
            errors.append(
                "Active PostgreSQL definition still uses REFCURSOR for "
                f"{routine} at {definition.path.relative_to(ROOT)}:{definition.line_number} "
                f"({definition.returns})"
            )

        if postgres_name in TABLE_REQUIRED_ROUTINES and is_table_return(definition.returns) is False:
            errors.append(
                "Active PostgreSQL definition must return TABLE for "
                f"{routine} at {definition.path.relative_to(ROOT)}:{definition.line_number} "
                f"({definition.returns})"
            )

        if postgres_name in PASSWORDHASH_512_ROUTINES and "passwordhash VARCHAR(512)" not in definition.returns:
            errors.append(
                "Active PostgreSQL user contract must expose passwordhash VARCHAR(512) for "
                f"{routine} at {definition.path.relative_to(ROOT)}:{definition.line_number} "
                f"({definition.returns})"
            )

    return errors


def validate_command_helper_contract() -> list[str]:
    text = read_text(DB_COMMAND_HELPER)
    errors: list[str] = []

    expected_fragments = [
        'return $"public.{baseName.ToLowerInvariant()}";',
        'SELECT * FROM {command.CommandText}({parameterList});',
        'SELECT {command.CommandText}({parameterList});',
        '"p_" + parameterName.TrimStart(\'@\').ToLowerInvariant()',
    ]

    for fragment in expected_fragments:
        if fragment not in text:
            errors.append(f"DbCommandHelper contract fragment missing: {fragment}")

    output_fragments = [
        "Any(parameter => parameter.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue)",
        "NormalizeParameterName(DatabaseProviderNames.PostgreSQL, parameter.ParameterName)",
        "parameter.Value = reader.IsDBNull(ordinal) ? DBNull.Value : reader.GetValue(ordinal);",
    ]

    for fragment in output_fragments:
        if fragment not in text:
            errors.append(f"DbCommandHelper PostgreSQL output-parameter fragment missing: {fragment}")

    return errors


def validate_repository_execution_paths() -> list[str]:
    errors: list[str] = []
    direct_execute_pattern = re.compile(r"(?<!DbCommandHelper\.)Execute(?:Reader|Scalar|NonQuery)Async\s*\(")

    for path in INFRA_ROOT.rglob("*.cs"):
        if any(part in {"bin", "obj"} for part in path.parts):
            continue
        if path == DB_COMMAND_HELPER:
            continue

        text = read_text(path)
        for match in direct_execute_pattern.finditer(text):
            line_number = text[: match.start()].count("\n") + 1
            errors.append(f"Direct command execution bypasses DbCommandHelper in {path.relative_to(ROOT)}:{line_number}")

    return errors


def validate_no_next_result_usage() -> list[str]:
    errors: list[str] = []
    next_result_pattern = re.compile(r"\bNextResultAsync\s*\(")
    runtime_roots = [
        BACKEND_ROOT / "GoWithFlow.Infrastructure",
        BACKEND_ROOT / "GoWithFlow.Application",
        BACKEND_ROOT / "GoWithFlow.API",
    ]

    for root in runtime_roots:
        for path in root.rglob("*.cs"):
            if any(part in {"bin", "obj"} for part in path.parts):
                continue

            text = read_text(path)
            for match in next_result_pattern.finditer(text):
                line_number = text[: match.start()].count("\n") + 1
                errors.append(
                    f"NextResultAsync is not provider-safe and is still present in {path.relative_to(ROOT)}:{line_number}"
                )

    return errors


def validate_refresh_token_fix() -> list[str]:
    errors: list[str] = []
    file_06 = PG_MIGRATION_ROOT / "06_stored_procedures.sql"
    file_11 = PG_MIGRATION_ROOT / "11_auth_routine_fixes.sql"

    text_06 = read_text(file_06) if file_06.exists() else ""
    text_11 = read_text(file_11) if file_11.exists() else ""

    if "CREATE OR REPLACE FUNCTION usprovkerefreshtoken(" in text_06 and "CREATE OR REPLACE FUNCTION usprevokerefreshtoken(" not in text_11:
        errors.append("Refresh-token revoke typo exists in 06_stored_procedures.sql but 11_auth_routine_fixes.sql does not add usprevokerefreshtoken.")

    return errors


def main() -> int:
    validators = [
        validate_routine_mapping,
        validate_active_postgres_contracts,
        validate_command_helper_contract,
        validate_repository_execution_paths,
        validate_no_next_result_usage,
        validate_refresh_token_fix,
    ]

    errors: list[str] = []
    for validator in validators:
        errors.extend(validator())

    if errors:
        print("Dual-provider contract validation failed.")
        for error in errors:
            print(error)
        return 1

    print("Dual-provider contract validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
