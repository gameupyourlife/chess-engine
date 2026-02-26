# chess-engine

This project is a UCI-compatible chess bot written in C#.

It was created together with a friend while we were on an academy of the CDE e.V.

## What it does

- Accepts standard UCI commands through standard input
- Evaluates positions
- Returns moves in UCI notation (`bestmove ...`)

## How to use the chess bot

You can use the bot either from source or by downloading the executable from the GitHub Releases page.

### Option 1: Run from source

From the repository root:

```bash
dotnet run --project /home/runner/work/chess-engine/chess-engine/chess-engine/chess-engine.csproj
```

### Option 2: Run the released executable

Download the latest executable from Releases and run it from a terminal.

## UCI quick start

After starting the bot, send commands such as:

```text
uci
isready
ucinewgame
position startpos
go
quit
```

Typical responses include:

- `uciok`
- `readyok`
- `bestmove e2e4` (example)
