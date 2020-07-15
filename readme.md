# HierarchyGrid

 

This component aims to display data along two different hierarchical classifications, as if a datagrid had its rows and columns defined by treeviews.

 

## Definitions

Rows and columns are defined by *HierarchyDefinition*. There are two kinds of these definitions:

- *ProducerDefinition*: provides an object, by default on row level.

- *ConsumerDefinition*: extracts information from the object received from the producer, by default on column level.

 

```csharp

public object Content { get; set; }

```

Defines the content of the grid header associated to the definition.

 

```csharp

public X Add<X>(X child) where X : HierarchyDefinition

```

Adds a properly set child element to the current hierarchy definition.

 

### ProducerDefinition

```csharp

public Func<object> Producer { get; set; }

```

Defines the function that will be used to retrieve data from the producer.

 

```csharp

public Func<Qualification> Qualify { get; set; }

```

Allows override of qualification for all elements produced by this definition, independently from the consumer definition.

 

### ConsumerDefinition

```csharp

public Func<object, object> Consumer { get; set; }

```

Defines the function that will extract information from the provided data.

 

```csharp

public Func<object, string> Formatter { get; set; }

```

Defines the function that will format the consumer result as it will be displayed in the grid.

 

```csharp

public Func<object, Qualification> Qualify { get; set; }

```

Defines the function that will set the cell qualification according to consumer result.

 

```csharp

public Func<object, (byte a, byte r, byte g, byte b)> Colorize { get; set; }

```

Defines the function that will set the cell background if cell qualification is set to ***Custom***.

 

```csharp

public Func<object, object, string, bool> Editor { get; set; }

```

Defines the function called when a cell edition is validated. The first parameter is the object provided by the producer, the second is the result from the consumer and the third one is the string input from the editing textbox. The boolean returns whether or not data has been updated.

 

## Resulting grid

 

### Qualifications

Cell rendering can be modified according to a qualification. Available values are: Unset, Empty, Normal, Error, Warning, Remark, Custom, ReadOnly.

 

## Known issues

Even if UI performance has always been a major focus, *bindings* do impact scrolling speed. The more cells are displayed, the more the component will slow down.