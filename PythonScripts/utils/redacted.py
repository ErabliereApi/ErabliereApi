def redacted(text: str, terms: list[str]) -> str:
    for term in terms:
        text = text.replace(term, "<redacted>")
    return text