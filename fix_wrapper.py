with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "r") as f:
    content = f.read()

# Update EntityWrapper to forward events
# Note: we need to use INotifyPropertyChanged.

wrapper_old = """    private class EntityWrapper
    {
        private readonly Entity _entity;

        public EntityWrapper(Entity entity)
        {
            _entity = entity;
        }

        [System.ComponentModel.Category("Entity")]
        public string Name
        {
            get => _entity.Name;
            set => _entity.Name = value;
        }

        [System.ComponentModel.Category("Entity")]
        public bool IsEnabled
        {
            get => _entity.IsEnabled;
            set => _entity.IsEnabled = value;
        }

        [System.ComponentModel.Category("Transform")]
        [System.ComponentModel.TypeConverter(typeof(Vector2Converter))]
        public Vector2 Position
        {
            get => _entity.Transform.Value.Position;
            set => _entity.Transform.Value.Position = value;
        }

        [System.ComponentModel.Category("Transform")]
        public float Rotation
        {
            get => _entity.Transform.Value.Rotation;
            set => _entity.Transform.Value.Rotation = value;
        }

        [System.ComponentModel.Category("Transform")]
        [System.ComponentModel.TypeConverter(typeof(Vector2Converter))]
        public Vector2 Scale
        {
            get => _entity.Transform.Value.Scale;
            set => _entity.Transform.Value.Scale = value;
        }
    }"""

wrapper_new = """    private class EntityWrapper : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly Entity _entity;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public EntityWrapper(Entity entity)
        {
            _entity = entity;
            _entity.Transform.Value.PropertyChanged += (s, e) => PropertyChanged?.Invoke(this, e);
        }

        [System.ComponentModel.Category("Entity")]
        public string Name
        {
            get => _entity.Name;
            set
            {
                if (_entity.Name != value)
                {
                    _entity.Name = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        [System.ComponentModel.Category("Entity")]
        public bool IsEnabled
        {
            get => _entity.IsEnabled;
            set
            {
                if (_entity.IsEnabled != value)
                {
                    _entity.IsEnabled = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }
        }

        [System.ComponentModel.Category("Transform")]
        [System.ComponentModel.TypeConverter(typeof(Vector2Converter))]
        public Vector2 Position
        {
            get => _entity.Transform.Value.Position;
            set => _entity.Transform.Value.Position = value;
        }

        [System.ComponentModel.Category("Transform")]
        public float Rotation
        {
            get => _entity.Transform.Value.Rotation;
            set => _entity.Transform.Value.Rotation = value;
        }

        [System.ComponentModel.Category("Transform")]
        [System.ComponentModel.TypeConverter(typeof(Vector2Converter))]
        public Vector2 Scale
        {
            get => _entity.Transform.Value.Scale;
            set => _entity.Transform.Value.Scale = value;
        }
    }"""

content = content.replace(wrapper_old, wrapper_new)

with open("2dGameEngine/2dGameEngine/2dGameEngine/MainForm.cs", "w") as f:
    f.write(content)
