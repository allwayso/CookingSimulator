import json
import sys
from pathlib import Path
from urllib import error, request


ROOT = Path(__file__).resolve().parents[1]
CONFIG_PATH = ROOT / "ai_review.local.json"
DEFAULT_BASE_URL = "https://api.openai.com/v1"


def load_providers():
    if not CONFIG_PATH.exists():
        raise FileNotFoundError(f"Missing config: {CONFIG_PATH}")

    data = json.loads(CONFIG_PATH.read_text(encoding="utf-8-sig"))
    if isinstance(data, dict) and "providers" in data:
        providers = data["providers"]
    elif isinstance(data, dict):
        providers = [data]
    else:
        raise ValueError("Config must be an object or an object with a providers array.")

    normalized = []
    for index, provider in enumerate(providers, start=1):
        normalized.append(
            {
                "name": provider.get("name") or f"provider_{index}",
                "baseUrl": provider.get("baseUrl") or DEFAULT_BASE_URL,
                "apiKey": provider.get("apiKey", ""),
                "model": provider.get("model", ""),
            }
        )
    return normalized


def build_url(base_url):
    base = base_url.rstrip("/")
    return base if base.endswith("/chat/completions") else f"{base}/chat/completions"


def test_provider(provider):
    if not provider["apiKey"] or not provider["model"]:
        return False, "missing apiKey or model"

    payload = {
        "model": provider["model"],
        "messages": [{"role": "user", "content": "只回复 OK。"}],
        "temperature": 0.0,
    }
    body = json.dumps(payload).encode("utf-8")
    req = request.Request(build_url(provider["baseUrl"]), data=body, method="POST")
    req.add_header("Content-Type", "application/json")
    req.add_header("Authorization", f"Bearer {provider['apiKey']}")

    try:
        with request.urlopen(req, timeout=30) as response:
            text = response.read().decode("utf-8", errors="replace")
            return bool(text.strip()), f"HTTP {response.status}"
    except error.HTTPError as exc:
        return False, f"HTTP {exc.code}"
    except error.URLError as exc:
        return False, f"network error: {exc.reason}"
    except Exception as exc:
        return False, f"unexpected error: {exc}"


def main():
    try:
        providers = load_providers()
    except Exception as exc:
        print(f"Config error: {exc}")
        return 1

    failures = []
    for provider in providers:
        success, message = test_provider(provider)
        print(f"{provider['name']}: {message}")
        if success:
            print("API test passed.")
            return 0
        failures.append(f"{provider['name']} -> {message}")

    print("API test failed.")
    for failure in failures:
        print(failure)
    return 1


if __name__ == "__main__":
    sys.exit(main())
