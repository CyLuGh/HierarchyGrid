namespace HierarchyGrid.Definitions;

/// <summary>
/// Represents the qualification status of an input or element within a hierarchical grid structure.
/// </summary>
/// <remarks>
/// The <see cref="Qualification"/> enum defines various states or attributes that can be assigned
/// to elements, inputs, or conditions in the grid. This enables efficient categorization and highlights
/// specific behavior or evaluations based on the current context.
/// </remarks>
public enum Qualification
{
    Unset,
    Empty,
    Normal,
    Error,
    Warning,
    Remark,
    Custom,
    ReadOnly,
    Computed,
    Highlighted,
    Hovered,
}
