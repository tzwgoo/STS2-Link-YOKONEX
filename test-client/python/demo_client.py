import json
import threading
import time

import requests
import websocket

BASE_URL = "http://127.0.0.1:15526"
TOKEN = "change-me"


def headers():
    return {"X-STS2-Token": TOKEN}


def show_health():
    response = requests.get(f"{BASE_URL}/health", timeout=3)
    print("HEALTH:", response.json())


def show_state():
    response = requests.get(f"{BASE_URL}/state", headers=headers(), timeout=3)
    print("STATE:", json.dumps(response.json(), ensure_ascii=False, indent=2))


def end_turn():
    payload = {
        "requestId": "req-end-turn-001",
        "action": "end_turn",
        "params": {},
    }
    response = requests.post(
        f"{BASE_URL}/action",
        headers={**headers(), "Content-Type": "application/json"},
        data=json.dumps(payload),
        timeout=3,
    )
    print("ACTION:", response.json())


def run_ws():
    def on_message(ws, message):
        print("WS:", message)

    ws = websocket.WebSocketApp(
        "ws://127.0.0.1:15526/ws",
        header=[f"X-STS2-Token: {TOKEN}"],
        on_message=on_message,
    )
    ws.run_forever()


if __name__ == "__main__":
    show_health()
    try:
        show_state()
    except Exception as exc:
        print("STATE ERROR:", exc)

    ws_thread = threading.Thread(target=run_ws, daemon=True)
    ws_thread.start()
    time.sleep(1)

    try:
        end_turn()
    except Exception as exc:
        print("ACTION ERROR:", exc)

    time.sleep(3)
