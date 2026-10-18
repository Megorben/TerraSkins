"""Decompiles tModLoader types with ilspycmd and prints the members listed in a query file.

Query lines:
  Type::Member        print every declaration of Member with its body
  Type::Member::grep  print only lines of Member's body that match the regex after the second ::
  Type                print every public member signature of Type
"""
import re
import subprocess
import sys

dll, queries = sys.argv[1], sys.argv[2]
cache = {}


def source(type_name):
    if type_name not in cache:
        out = subprocess.run(["ilspycmd", dll, "-t", type_name], capture_output=True, text=True)
        cache[type_name] = out.stdout
    return cache[type_name]


def blocks(text, member):
    pattern = re.compile(r"^\s*(?:\[[^\n]*\]\s*)*(?:(?:public|private|internal|protected|static|override|virtual|readonly|unsafe|extern|abstract|sealed|new|ref)\s+)+[^\n;=]*?\b" + re.escape(member) + r"\b\s*(?:\(|\{|=|;)", re.M)
    for m in pattern.finditer(text):
        start = text.rfind("\n", 0, m.start()) + 1
        i = m.end()
        # Fields and expression bodies end at the first semicolon, methods at the matching brace.
        semi = text.find(";", i)
        brace = text.find("{", i)
        if brace == -1 or (semi != -1 and semi < brace and "(" not in text[m.start():semi] ):
            yield text[start:semi + 1]
            continue
        if semi != -1 and semi < brace and text[i - 1] != "{":
            # e.g. abstract method or delegate field
            if text[start:semi].count("(") == text[start:semi].count(")"):
                yield text[start:semi + 1]
                continue
        depth = 0
        j = brace
        while j < len(text):
            if text[j] == "{":
                depth += 1
            elif text[j] == "}":
                depth -= 1
                if depth == 0:
                    break
            j += 1
        yield text[start:j + 1]


for line in open(queries):
    line = line.strip()
    if not line or line.startswith("#"):
        continue
    parts = line.split("::")
    text = source(parts[0])
    print(f"\n==================== {line}")
    if len(parts) == 1:
        for l in text.splitlines():
            if re.match(r"^\t(public|protected)", l):
                print(l.strip())
        continue
    found = False
    for body in blocks(text, parts[1]):
        found = True
        if len(parts) == 3:
            for n, l in enumerate(body.splitlines()):
                if re.search(parts[2], l):
                    print(f"{n:5}: {l.rstrip()}")
            print("-----")
        else:
            body_lines = body.splitlines()
            if len(body_lines) > 400:
                print("\n".join(body_lines[:400]))
                print(f"... ({len(body_lines)} lines)")
            else:
                print(body)
    if not found:
        print("NOT FOUND")
