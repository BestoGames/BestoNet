![BestoNet](assets/BESTO_NET.png)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat)](docs/CONTRIBUTING.md)
[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/bryanpaik/isd-rollback/graphs/commit-activity)

BestoNet is a custom rollback networking solution developed for the fighting game [Idol Showdown](https://store.steampowered.com/app/1742020/Idol_Showdown/). Built in C# for Unity, it integrates with Facepunch Steamworks to provide a robust and efficient rollback implementation.

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

### Essential Components
1. **Frame Timing System**
    - Requires precise frame length control
    - Current implementation in IS uses a separate thread for accurate timing

2. **State Management**
    - Save/Load state system
    - Requires deterministic game logic
    - Recommendation: Avoid using byte[] with BinaryWriter/Reader due to inefficiency

### Additional Systems Needed
- **Spectator System**: Framework prepared with confirm frame input buffer
- **Desync Detection**: Requires custom implementation
- **Demo Recording**: Suggested implementation through confirmed input recording
- **Game Synchronization**: Currently uses first 10 frames for basic synchronization

## Configuration

![Rollback Settings](assets/rollback_settings.png)

**Current Unity Configuration Settings**

**Note**: The MaxRollbackFrames setting currently shows 4 frames but effectively provides ~8 frames of rollback in Idol Showdown. This discrepancy is known but functional.

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
