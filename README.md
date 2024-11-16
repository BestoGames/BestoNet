# BestoNet
### What is Besto Net
ISD Rollback is a custom rollback system made for Idol Showdown, this is written in C# Unity and utilizes Facepunch Steamworks.

Here's our steam page!
https://store.steampowered.com/app/1742020/Idol_Showdown/

### Why was this made
Originally we used Unity GGPO but there were some issues.
1. In long distance connections, connections were unplayable due to having around 7 frames of rollback
2. GGPO is in C++, this makes it harder to mantain since its using a different language
3. In our setup, GGPO would cause hard crashes in bad network conditions

### Features
1. Speculative saving, during rollbacks it will only save certain frames of the rollback. Right now it's set to the midpoint and the confirm frame, it will also always save at the end of the frame
2. Input prediction (it will repeat the last received input), if the predicted frame is correct it will not perform a rollback
3. Rift management, has many options to tune and deal with one sided rollback
4. Reliable input messaging, current set to send the past 7 frames to account for lost packets/out of order packets

### What you need
1. Some way to extend your frame length, we are using a seperate thread as a timer to get accurate frame timing
2. Spectator! this is not hooked up to a spectator system, but there is a call to send confirm frame inputs to a spectator buffer
3. Desync manager, is not a part of this so that would need to be made as well
4. Demo System, also needs to be made probably just need to save the confirmed inputs
5. Save/Load states, you need to have a deterministic game, I reccommend not using the byte[] system like we did since BinaryWrite/Read is very inefficient and hard to work with.
6. No syncing system at the moment thats included, right now it just uses the first 10 frames to sync up the games
Obviously there is alot more things you need to make a full fighting game, but this is how you would integrate this system

## Current Unity Settings
![image](https://github.com/user-attachments/assets/f78cfd7d-f72e-4138-a018-2a37c12a43f6)

This will need to be adjusted depending on your needs. Something to note is that MaxRollbackFrames isn't accurate, it's set to 4 which in IS is equivalent to ~8 RF. It works good enough, so I never bothered to fix it yet.

## Credits
This takes inspiration from the MK/Injustice GDC talk, Zinac's youtube rollback guide, GekkoNet's Rollback System.
Also, this wouldn't be possible without RinIota, who built the original networking system, which helped as a foundation to build this new rollback system

