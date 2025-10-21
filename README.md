# AES67 Receiver (.NET / C#)

A lightweight C# library and example console application for discovering and receiving **AES67 RTP audio streams** over IP networks.

It demonstrates:
- SAP / SDP discovery
- RTP packet receiving
- L24 PCM audio decoding
- Conversion to IEEE Float 32
- Real-time playback using NAudio

---
<img width="893" height="356" alt="image" src="https://github.com/user-attachments/assets/886e6461-a64c-467e-a605-b34b806821ad" />

---
## 🧩 Dependencies

| Package | Description |
|----------|--------------|
| `NAudio` | Audio I/O and sample handling |
| `Microsoft.Extensions.Logging` | Structured logging support |
| `System.Net` | UDP / RTP network handling |
| `System.Collections.Concurrent` | Thread-safe queues for packet buffering |

You can install dependencies with:
```bash
dotnet add package NAudio
dotnet add package Microsoft.Extensions.Logging.Console

