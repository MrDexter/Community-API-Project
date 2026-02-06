Documentation:

**/players**
Params: Limit & Offset
Lists all players. Only shows limited data per player
Can be paginated with limit and offset, Not Required, can be used individually
Example: /players/ or /player?limit=5&offset=5

**/players/id**
Params: ID
List specific glayer by Database ID or Steam ID
Shows full list of player data, including housing and vehicles
Example: /player/7 or /player/76561198213433993

**/players/id/updaterank**
Params: Rank & NewRank & JWT Token
Update a players rank
Example: /players/7?rank=coplevel&newrank=10

**/gangs**
Params: Limit & Offset
Lists all gangs.
Can be paginated with limit and offset, Not Required, can be used individually
Example: /gangs/ or /gangs?limit=5

**/gangs/id**
Params: ID
List specific gang by Database ID
Shows full list of gang data, including housing
Example: /gangs/5996

**/Auth/Token**
Params: Name & Group & Perms & X-Auth-Secret
Generates a JWT Token with specified params. Requires Secret to match from appsettings
Example: /Auth/Token?name=api-client&group=police&perms=write
