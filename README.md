# McHoneypot Configuration Guide

The `config.json` file controls the behavior of the honeypot, including network settings, fake server status presentation, and tarpit defense mechanisms. All keys are formatted in `snake_case`.

## General Server Configuration

These parameters define the core network bindings and the fake server metadata returned to scanning clients.

| Parameter                  | Type    | Default Value                                   | Description                                                                                                        | Possible Values                                                      |
|:---------------------------|:--------|:------------------------------------------------|:-------------------------------------------------------------------------------------------------------------------|:---------------------------------------------------------------------|
| `log_level`                | String  | `"Information"`                                 | The level of the logs.                                                                                             | Trace, Debug, Information, Warning, Error, Critical, None (disabled) |
| `bind_address`             | String  | `"0.0.0.0"`                                     | The IP address the server will bind to.                                                                            | Any valid IPv4 or IPv6 address.                                      |
| `port`                     | Integer | `25565`                                         | The port the server will listen on.                                                                                | `1` - `65535`                                                        |
| `version_name`             | String  | `"Paper 1.20.4"`                                | The fake server software version displayed to the client.                                                          | Any string.                                                          |
| `description`              | String  | `"§aVanilla Survival §c[1.20.4]§r\n§eWelcome!"` | The Server MOTD (Message of the Day). Supports standard Minecraft color codes (`§`).                               | Any string.                                                          |
| `timeout_ms`               | Integer | `10000`                                         | Connection timeout in milliseconds. Drops idle connections after this duration.                                    | Any positive integer.                                                |
| `protocol_behavior`        | String  | `"Chameleon"`                                   | Determines how the server responds to different client protocol versions.                                          | `"Chameleon"` (mimics client), `"Fixed"` (uses static version).      |
| `fixed_protocol_version`   | Integer | `765`                                           | The specific protocol version number to send back when `protocol_behavior` is not set to Chameleon (765 = 1.20.4). | Any integer representing a Minecraft protocol version.               |
| `max_client_packet_length` | Integer | `65536`                                         | Maximum allowed incoming packet length in bytes. Used to prevent buffer overflows from malicious payloads.         | Any positive integer.                                                |
| `max_players`              | Integer | `1000`                                          | The fake maximum player limit displayed in the server list.                                                        | Any integer.                                                         |
| `online_players`           | Integer | `874`                                           | The fake current online player count displayed in the server list.                                                 | Any integer.                                                         |

---

## Trap Configuration (`trap`)

This section controls the defense mechanisms (Tarpit) and the generation of fake player samples designed to confuse scanners or waste their resources.

| Parameter              | Type             | Default Value             | Description                                                                                                                                                                              | Possible Values                                           |
|:-----------------------|:-----------------|:--------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:----------------------------------------------------------|
| `fake_players_count`   | Integer          | `100`                     | The number of unique fake player profiles (names and UUIDs) generated for the player sample list.                                                                                        | Any positive integer.                                     |
| `enable_tarpit`        | Boolean          | `true`                    | Toggles the Tarpit mechanism to deliberately slow down responses to scanning bots.                                                                                                       | `true`, `false`                                           |
| `initial_delay_ms`     | Integer          | `5000`                    | The delay (in milliseconds) before sending the very first byte of the status response payload.  Works only if `enable_tarpit` = `true`                                                   | Any positive integer (e.g., `0` to disable initial wait). |
| `max_bytes_per_second` | Integer          | `5`                       | Artificial bandwidth throttling. Forces the attacker's scanner to read the JSON payload extremely slowly, keeping their connection pool occupied. Works only if `enable_tarpit` = `true` | Any integer greater than `0`.                             |
| `base_names`           | Array of Strings | `["Honda", "Brede", ...]` | A list of core names used as the foundation for randomly generating fake players.                                                                                                        | Any list of strings.                                      |
| `prefixes`             | Array of Strings | `["xX_", "Real_", ...]`   | A list of prefixes attached to the base names during fake player generation.                                                                                                             | Any list of strings.                                      |
| `suffixes`             | Array of Strings | `["_Xx", "1337", ...]`    | A list of suffixes attached to the base names during fake player generation.                                                                                                             | Any list of strings.                                      |

### Example `config.json`

```json
{
  "bind_address": "0.0.0.0",
  "port": 25565,
  "version_name": "Paper 1.20.4",
  "description": "§aVanilla Survival §c[1.20.4]§r\n§eWelcome!",
  "timeout_ms": 10000,
  "protocol_behavior": "Chameleon",
  "fixed_protocol_version": 765,
  "max_client_packet_length": 65536,
  "max_players": 1000,
  "online_players": 874,
  "trap": {
    "fake_players_count": 100,
    "enable_tarpit": true,
    "initial_delay_ms": 5000,
    "max_bytes_per_second": 5,
    "base_names": [
      "Honda", "Brede", "Titkta", "SlopEd", "Brudd", "Jerr3", "Kokonito", "Franc", "BodyKamobebady", "HellDi"
    ],
    "prefixes": [
      "xX_", "Real_", "Pro_", "Super", "MC_", "Itz_", "1", "2"
    ],
    "suffixes": [
      "_Xx", "1337", "HD", "YT", "Gamer", "_Pro", "228", "SPUN"
    ]
  }
}