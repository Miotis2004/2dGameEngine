with open("2dGameEngine/2dGameEngine/2dGameEngine/Core/Entity.cs", "r") as f:
    content = f.read()

old_remove = """    public bool RemoveComponent(Component component)
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
    }"""

new_remove = """    public bool RemoveComponent(Component component)
    {
        ArgumentNullException.ThrowIfNull(component);
        bool existed = _components.Contains(component) && !_componentsToRemove.Contains(component);
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
    }"""

content = content.replace(old_remove, new_remove)

with open("2dGameEngine/2dGameEngine/2dGameEngine/Core/Entity.cs", "w") as f:
    f.write(content)
