# Godot 4 Multiplayer (Authoritative Server) Demo + Single Player 3D Character Controller

This project demonstrates a authoritative multiplayer setup and a single-player character controller using the latest version of Godot 4 and .NET 6. 

I don't intend to continue work on this project since I've moved on to other things. But, I've published it in case someone else might find it useful.

**Note**: This project was developed for Godot Beta 2.0 and may not be compatible with later versions of Godot 4.

## Multiplayer Features

- Client-side prediction of player entities
- Client-side interpolation of remote entities
- Backwards reconciliation and replay
- Server standalone
- Network Sync Vars
- Real-time adjustment of client simulation speed to optimize server's input buffer (Overwatch's method).
- Server-side lag compensation
- Full godot server implementation with disabled 3d
- Master and multi clients in one project (split screen)
- Optimized netcode (Quake, Overwatch, Valve methods)
- Remote de(activation) of player components
- Server variable sharing between server and client (ServerVars)
- RCON Implementation for Server Management

## Acknowledgments

This project is inspired from the following projects:

- [Open Source Shooter](https://git.join-striked.com/striked-gaming/open-source-shooter)
- [Open 3D mannequin](https://github.com/GDQuest/godot-3d-mannequin)

## Screenshots

### Multiplayer Demo

![acruxx multiplayer example](.docs/assets/acruxx-multiplayer-demo.gif)

### Player controller

![acruxx demo](.docs/assets/acruxx-player-demo.gif)