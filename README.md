I have built this as a reference backend project based off a similar system I previously made use of. It shows API design, authentication and database implementation using C# and .NET

Documentation:

## GET /players
Lists all players. Only shows limited data per player

**Params:** 
- Limit
- Offset

**Example:**
- /players/
- /players?limit=5&offset=5

## GET /players/id
List specific player by Database ID or Steam ID
Shows full list of player data, including housing and vehicles

**Params:**
- ID

**Example:**
- /players/7
- /players/76561198213433993

## POST /players/id/updaterank
Update a players rank

**Params:** 
- Rank
- NewRank
- JWT Token

**Example:**
- /players/7?rank=coplevel&newrank=10

## GET /gangs
Lists all gangs.

**Params:**
- Limit
- Offset

**Example:**
- /gangs/
- /gangs?limit=5

## GET /gangs/id
List specific gang by Database ID
Shows full list of gang data, including housing

**Params:** 
- ID

**Example:**
- /gangs/5996

## GET /Auth/Token
Generates a JWT Token with specified params. Requires Secret to match from appsettings

**Params:**
- Name
- Group
- Perms
- X-Auth-Secret

**Example:**
- /Auth/Token?name=api-client&group=police&perms=write
