<img src="assets/bestoneto.png" width="300" />

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat)](docs/CONTRIBUTING.md)
[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/bryanpaik/isd-rollback/graphs/commit-activity)

BestoNet is a custom rollback networking solution developed for the fighting game [Idol Showdown](https://store.steampowered.com/app/1742020/Idol_Showdown/). Built in C# for Unity, it integrates with Facepunch Steamworks to provide a robust and efficient rollback implementation.

This is a work in progress as a library, and won't work right out the box

## 📚 Documentation

- [Contributing Guide](docs/CONTRIBUTING.md)
- [Code of Conduct](docs/CODE_OF_CONDUCT.md)
- [Development Roadmap](docs/ROADMAP.md)

## Background

Originally we used Unity GGPO but there were some issues.
- Unplayable connections at long distances (experiencing ~7 frames of rollback)
- Maintenance difficulties due to GGPO's C++ codebase
- System instability during poor network conditions
- Using GGPO required a relay system that added extra latency to online matches

## Core Features

### Rollback Management
- **Speculative Frame Saving**: Saves CPU time and preventing 7 frame rollbacks
    - Currently saves the midpoint frame, confirm frame, and end frame
- **Input Prediction**: Will predict the next input using the previously received input
- **Rift Management**: Many different options to deal with one sided rollback and game syncing
- **Reliable Input Messaging**: Maintains a 7-frame input buffer to handle packet loss and out-of-order delivery

## Implementation Requirements

1. Frame Timing: Idol Showdown currently uses a separate thread as a timer.
2. Spectator: The current implementation contains a call to send confirmed frame inputs to a spectator buffer
3. State management system: Deterministic game logic is required. For efficiency reasons, we recommend avoiding using byte[] with BinaryWriter/Reader

## Configuration

![Rollback Settings](assets/rollback_settings.png)

**Current Unity Configuration Settings**

## Credits

This implementation draws inspiration from:
- MK/Injustice GDC presentation
- Zinac's rollback implementation guide
- GekkoNet's Rollback System

Special thanks to [Rin Iota](https://x.com/sss_iota_sss) for developing the original networking foundation that enabled ISD Rollback's creation.

## Contributing

This is an open-source project accepting community contributions. Please read our [Contributing Guide](docs/CONTRIBUTING.md) and [Code of Conduct](docs/CODE_OF_CONDUCT.md) before submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
