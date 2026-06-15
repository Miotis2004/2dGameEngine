import re

with open("2dGameEngine/2dGameEngine/2dGameEngine/Core/Entity.cs", "r") as f:
    content = f.read()

content = content.replace("private readonly List<Component> _componentsToAdd = [];",
                          "private readonly List<Component> _componentsToAdd = [];\n    private readonly List<Component> _componentsToRemove = [];")

remove_method = """    /// <summary>
    /// Removes a component from this entity.
    /// </summary>
    /// <param name="component">The component to remove.</param>
    /// <returns><see langword="true"/> when the component was present and removed.</returns>
    public bool RemoveComponent(Component component)
    {
        ArgumentNullException.ThrowIfNull(component);
        bool existed = _components.Remove(component);
        bool pending = _componentsToAdd.Remove(component);
        if (!existed && !pending)
        {
            return false;
        }

        if (existed)
        {
            _componentsToRemove.Add(component);
        }

        component.Detach();
        return true;
    }
"""

idx = content.find("public TComponent? GetComponent<TComponent>()")
content = content[:idx] + remove_method + "\n    " + content[idx:]

structural_update = """        if (_componentsToRemove.Count > 0)
        {
            foreach (Component comp in _componentsToRemove)
            {
                _components.Remove(comp);
            }
            _componentsToRemove.Clear();
        }"""

idx2 = content.find("if (_componentsToAdd.Count > 0)")
content = content[:idx2] + structural_update + "\n\n        " + content[idx2:]

with open("2dGameEngine/2dGameEngine/2dGameEngine/Core/Entity.cs", "w") as f:
    f.write(content)

with open("2dGameEngine/2dGameEngine/2dGameEngine/Core/Component.cs", "r") as f:
    content2 = f.read()

detach_method = """    internal void Detach()
    {
        Entity = null;
        OnDetached();
    }

    /// <summary>
    /// Called after the component is detached from an entity.
    /// </summary>
    protected virtual void OnDetached()
    {
    }
"""

idx3 = content2.find("protected virtual void OnAttached()")
idx4 = content2.find("}", idx3) + 1
content2 = content2[:idx4] + "\n\n" + detach_method + content2[idx4:]

with open("2dGameEngine/2dGameEngine/2dGameEngine/Core/Component.cs", "w") as f:
    f.write(content2)
