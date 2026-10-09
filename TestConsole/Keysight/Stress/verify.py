"""Интеграционные проверки собранной утилиты на локальном SCPI-сервере."""
import json
from pathlib import Path
import socket
import subprocess
import tempfile
import threading
import time
import unittest

PROJECT = Path(__file__).resolve().parents[2] / "TestConsole.csproj"
DLL = Path(subprocess.check_output(
    ["dotnet", "msbuild", str(PROJECT), "-getProperty:TargetPath", "-p:Configuration=Release", "-nologo"],
    text=True).strip())


class StressTests(unittest.TestCase):
    def run_case(self, scenario):
        commands = []
        failures = []
        finished = threading.Event()
        listener = socket.socket()
        listener.bind(("127.0.0.1", 0))
        listener.listen()
        listener.settimeout(0.1)
        port = listener.getsockname()[1]
        measurements = 0

        def serve():
            nonlocal measurements
            try:
                while not finished.is_set():
                    try:
                        conn, _ = listener.accept()
                    except socket.timeout:
                        continue
                    with conn:
                        conn.settimeout(0.1)
                        pending = b""
                        while not finished.is_set():
                            try:
                                data = conn.recv(4096)
                            except socket.timeout:
                                continue
                            if not data:
                                break
                            pending += data
                            while b"\n" in pending:
                                raw, pending = pending.split(b"\n", 1)
                                command = raw.decode().strip()
                                commands.append(command)
                                if command == "CONF:CAP":
                                    continue
                                if command == "*IDN?":
                                    response = "Other,123" if scenario == "wrong_model" else "Keysight Technologies,34465A,TEST,1"
                                elif command == "FUNC?":
                                    response = '"CAP"'
                                elif command == "MEAS:CAP?":
                                    measurements += 1
                                    if scenario == "silent" or (scenario == "timeout" and measurements <= 2):
                                        continue
                                    if scenario == "disconnect" and measurements == 1:
                                        conn.shutdown(socket.SHUT_RDWR)
                                        break
                                    response = "bad" if scenario == "invalid" else "+9.90000000E+37" if scenario == "overload" else "+1.47392365E-09"
                                else:
                                    raise AssertionError(command)
                                encoded = (response + "\r\n").encode()
                                if scenario == "fragmented":
                                    conn.sendall(encoded[:4])
                                    time.sleep(0.02)
                                    conn.sendall(encoded[4:])
                                else:
                                    conn.sendall(encoded)
                            else:
                                continue
                            break
            except Exception as error:
                failures.append(error)

        server = threading.Thread(target=serve, daemon=True)
        server.start()
        try:
            with tempfile.TemporaryDirectory() as directory:
                log = Path(directory) / "run.jsonl"
                result = subprocess.run(
                    ["dotnet", str(DLL), "keysight-stress", "127.0.0.1", "--port", str(port),
                     "--timeout", "300", "--cycles", "1", "--log", str(log)],
                    capture_output=True, timeout=15)
                events = [json.loads(line) for line in log.read_text(encoding="utf-8").splitlines()]
        finally:
            finished.set()
            server.join(timeout=2)
            listener.close()
        self.assertFalse(server.is_alive())
        self.assertEqual(failures, [])
        return result.returncode, commands, events

    def test_cases(self):
        for scenario in ["normal", "fragmented", "overload", "timeout", "disconnect", "wrong_model", "invalid", "silent"]:
            with self.subTest(scenario=scenario):
                code, commands, events = self.run_case(scenario)
                self.assertEqual(code, 0 if scenario in ["normal", "fragmented", "overload"] else 1)
                self.assertEqual(events[-1]["kind"], "SUMMARY")
                good = scenario not in ["wrong_model", "invalid", "silent"]
                self.assertEqual(any(e["kind"] == "MEASUREMENT" for e in events), good)
                if scenario == "timeout":
                    self.assertEqual(commands, ["*IDN?", "CONF:CAP", "FUNC?", "MEAS:CAP?", "MEAS:CAP?", "*IDN?", "MEAS:CAP?"])
                    self.assertEqual(sum(e["kind"] == "TIMEOUT" for e in events), 2)
                if scenario == "wrong_model":
                    self.assertEqual(commands, ["*IDN?"])
                if scenario == "overload":
                    self.assertTrue(any("overload=True" in e["message"] for e in events))

    def test_invalid_options(self):
        result = subprocess.run(["dotnet", str(DLL), "keysight-stress", "127.0.0.1", "--timeout", "0"], capture_output=True, timeout=5)
        self.assertEqual(result.returncode, 2)

    def test_menu_enters_stress_mode_and_returns(self):
        result = subprocess.run(["dotnet", str(DLL)], input="23\n\n0\n".encode(), capture_output=True, timeout=5)
        self.assertEqual(result.returncode, 0)
        self.assertIn("IP-адрес Keysight 34465A", result.stdout.decode("utf-8"))

    def test_direct_help(self):
        result = subprocess.run(["dotnet", str(DLL), "keysight-stress", "--help"], capture_output=True, timeout=5)
        self.assertEqual(result.returncode, 0)
        self.assertIn(b"TestConsole keysight-stress", result.stdout)


if __name__ == "__main__":
    unittest.main(verbosity=2)
