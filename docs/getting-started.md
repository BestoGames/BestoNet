# Getting Started

## Prerequisites
- Basic understanding of rollback networking concepts

## Installation

### Using Unity Package Manager
1. Open your project manifest.json
2. Add the following dependency:
```json
{
  "dependencies": {
    "com.bestogames.isd-rollback": "https://github.com/bryanpaik/isd-rollback.git"
  }
}
```

### Manual Installation
1. Download the latest release
2. Import the package into your Unity project

## Example Implementation

```csharp
// Basic implementation example here
```

## Common Issues and Solutions

1. Problem: Desyncs occurring frequently
   Solution: Ensure deterministic physics and random number generation

2. Problem: High memory usage
   Solution: Adjust speculative frame saving settings

## Next Steps

1. Check out the [API Documentation](API.md)
2. Join our [Discord](https://discord.gg/)
3. Read the [Contributing Guide](CONTRIBUTING.md)