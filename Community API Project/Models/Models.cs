namespace CommunityApi.Models;


public record Player (
    string Id,
    string Name,
    string PlayerId,
    string Cash,
    string Bankacc,
    string CartelCredits,
    string AdminLevel,
    string CopLevel,
    string IonLevel,
    string MedicLevel,
    DateTime LastSeen,
    DateTime InsertTime
);

public record Vehicles (
    string Id,
    string Side,
    string Class,
    string Type,
    string Inventory,
    string Reg,
    string Capacity,
    string Security,
    string Acceleration,
    DateTime InsertTime
);

public record Houses (
    string Id,
    string Location,
    string SecurityLevel,
    string VirtualContents,
    DateTime TimeBought
);

public record Gangs (
    string Id,
    string Name,
    string Members,
    string Leader,
    string Tag,
    string Bank
);

public record UpdateRankGet (
    string Name,
    string OldValue
);

public record UpdateRank (
    string Id,
    string Name,
    string RankName,
    string OldValue,
    string NewValue,
    string Outcome
);