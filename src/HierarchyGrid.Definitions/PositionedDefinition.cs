namespace HierarchyGrid.Definitions;

public readonly record struct PositionedDefinition(
    ElementCoordinates Coordinates,
    HierarchyDefinition Definition
);
