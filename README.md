# IS Rollback
## What is IS Rollback
IS Rollback is a custom rollback system made for Idol Showdown, this is written in C# Unity and utilizes Facepunch Steamworks.

## Why was this made
Originally we used Unity GGPO but there were some issues.
1. In long distance connections, connections were unplayable due to having around 7 frames of rollback
2. GGPO is in C++, this makes it harder to mantain since its using a different language
3. In our setup, GGPO would cause hard crashes in bad network conditions
