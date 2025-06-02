using System.Globalization;
using CAMAPI.DotnetHelper;
using CAMAPI.TechOperation;
using CAMAPI.UIDialogs.DotnetHelper;
using STCustomPropTypes;

namespace ExtensionNetProject;

/// <summary>
/// Temporary helper to add custom properties to operations. Simplifies code, but later CAMAPI provides more comfortable
/// way to add custom properties to operations.
/// </summary>
public class OperationCustomPropsHelper: IDisposable
{
    private readonly ComWrapper<IST_CustomPropHelpers> _propHelpersCom;

    /// <summary>
    /// Temporary helper to add custom properties to operations. Simplifies code, but later CAMAPI provides more comfortable
    /// way to add custom properties to operations.
    /// </summary>
    public OperationCustomPropsHelper()
    {
        _propHelpersCom = SystemExtensionFactory.GetSingletonExtension<IST_CustomPropHelpers>("Extension.CustomPropHelpers");;
    }
    
    /// <summary>
    /// Release COM objects
    /// </summary>
    public void Dispose()
    {
        _propHelpersCom.Dispose();
    }
    
    /// <summary>
    /// Add boolean parameter to operation
    /// </summary>
    public int AddBoolParam(ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<IST_SimplePropIterator> propIteratorCom,
        string caption,
        string paramId,
        bool defaultValue = false,
        int parentIndex = -1)
    {
        var operation = operationCom.Instance
                        ?? throw new Exception("OperationSolver container is not initialized");
        
        const string arrayId = "CustomOperationPropertiesArray";
        using var propCom = _propHelpersCom.InvokeAndWrap(propHelpers => propHelpers.CreateBooleanProp(caption));
        return propCom.Invoke(prop =>
        {
            var fullId = $"{arrayId}({paramId})";
            prop.PropID = fullId;
            prop.ValueGetter = new BooleanValueGetter(() =>
                operation.XMLProp.PropExists[fullId] &&
                operation.XMLProp.Str[fullId].Equals("true", StringComparison.OrdinalIgnoreCase));
            prop.ValueSetter = new BooleanValueSetter(v => operation.XMLProp.Str[fullId] = v ? "true" : "false");

            using var paramArrayCom = ComWrapper.Create(operation.XMLProp.Arr[arrayId]);
            if (!operation.XMLProp.PropExists[fullId])
            {
                var newItem = paramArrayCom.Invoke(paramArray => paramArray.CreateNewItem("CustomOperationProperty"));
                newItem.Str["Name"] = paramId;
                paramArrayCom.Invoke(paramArray => paramArray.AddItem(newItem));
            }
            operation.XMLProp.Str[fullId] = defaultValue ? "true" : "false";
            
            return propIteratorCom.Invoke(iterator => iterator.AddNewProp(prop, parentIndex));
        });
    }

    /// <summary>
    /// Add enum parameter to operation
    /// </summary>
    public int AddEnumWithIdParam(ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<IST_SimplePropIterator> propIteratorCom,
        string caption,
        string paramId,
        Dictionary<string, string> values,
        int parentIndex = -1)
    {
        var operation = operationCom.Instance
                        ?? throw new Exception("OperationSolver container is not initialized");
        
        const string arrayId = "CustomOperationPropertiesArray";
        using var propCom = _propHelpersCom.InvokeAndWrap(propHelpers => propHelpers.CreateEnumWithIDProp(caption));
        return propCom.Invoke(prop =>
        {
            var fullId = $"{arrayId}({paramId})";
            prop.PropID = fullId;
            prop.ValueGetter = new StringValueGetter(() => 
                operation.XMLProp.PropExists[fullId] ? operation.XMLProp.Str[fullId] : "");
            prop.ValueSetter = new StringValueSetter(value => operation.XMLProp.Str[fullId] = value);
            
            foreach (var value in values)
                prop.Add(value.Key, value.Value, "");
            
            using var paramArrayCom = ComWrapper.Create(operation.XMLProp.Arr[arrayId]);
            if (!operation.XMLProp.PropExists[fullId])
            {
                var newItem = paramArrayCom.Invoke(paramArray => paramArray.CreateNewItem("CustomOperationProperty"));
                newItem.Str["Name"] = paramId;
                paramArrayCom.Invoke(paramArray => paramArray.AddItem(newItem));
            }
            operation.XMLProp.Str[fullId] = values.FirstOrDefault().Key;
            
            return propIteratorCom.Invoke(iterator => iterator.AddNewProp(prop, parentIndex));
        });
    }

    public int AddDoubleProp(ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<IST_SimplePropIterator> propIteratorCom,
        string caption,
        string paramId,
        double defaultValue,
        IBooleanValueGetter? visible = null,
        int parentIndex = -1)
    {
        var operation = operationCom.Instance
                        ?? throw new Exception("OperationSolver container is not initialized");
        
        const string arrayId = "CustomOperationPropertiesArray";
        using var propCom = _propHelpersCom.InvokeAndWrap(propHelpers => propHelpers.CreateDoubleProp(caption));
        return propCom.Invoke(prop =>
        {
            var fullId = $"{arrayId}({paramId})";
            prop.PropID = fullId;
            prop.Visible = visible ?? new BooleanValueGetter(() => true);
            prop.ValueGetter = new DoubleValueGetter(() => operation.XMLProp.PropExists[fullId] ? operation.XMLProp.Flt[fullId] : defaultValue);
            prop.ValueSetter = new DoubleValueSetter(value => operation.XMLProp.Flt[fullId] = value);
            
            using var paramArrayCom = ComWrapper.Create(operation.XMLProp.Arr[arrayId]);
            if (!operation.XMLProp.PropExists[fullId])
            {
                var newItem = paramArrayCom.Invoke(paramArray => paramArray.CreateNewItem("CustomOperationProperty"));
                newItem.Str["Name"] = paramId;
                paramArrayCom.Invoke(paramArray => paramArray.AddItem(newItem));
            }
            operation.XMLProp.Str[fullId] = defaultValue.ToString(CultureInfo.InvariantCulture);
            
            return propIteratorCom.Invoke(iterator => iterator.AddNewProp(prop, parentIndex));
        });
    }

    public int AddIntegerProp(ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<IST_SimplePropIterator> propIteratorCom,
        string caption,
        string paramId,
        int defaultValue,
        IBooleanValueGetter? visible = null,
        int parentIndex = -1)
    {
        var operation = operationCom.Instance
                        ?? throw new Exception("OperationSolver container is not initialized");
        
        const string arrayId = "CustomOperationPropertiesArray";
        using var propCom = _propHelpersCom.InvokeAndWrap(propHelpers => propHelpers.CreateIntegerProp(caption));
        return propCom.Invoke(prop =>
        {
            var fullId = $"{arrayId}({paramId})";
            prop.PropID = fullId;
            prop.Visible = visible ?? new BooleanValueGetter(() => true);
            prop.ValueGetter = new IntegerValueGetter(() => operation.XMLProp.PropExists[fullId] ? operation.XMLProp.Int[fullId] : defaultValue);
            prop.ValueSetter = new IntegerValueSetter(value => operation.XMLProp.Int[fullId] = value);
            
            using var paramArrayCom = ComWrapper.Create(operation.XMLProp.Arr[arrayId]);
            if (!operation.XMLProp.PropExists[fullId])
            {
                var newItem = paramArrayCom.Invoke(paramArray => paramArray.CreateNewItem("CustomOperationProperty"));
                newItem.Str["Name"] = paramId;
                paramArrayCom.Invoke(paramArray => paramArray.AddItem(newItem));
            }
            operation.XMLProp.Str[fullId] = defaultValue.ToString();
            
            return propIteratorCom.Invoke(iterator => iterator.AddNewProp(prop, parentIndex));
        });
    }

    public int AddComplexProp(ComWrapper<IST_SimplePropIterator> propIteratorCom,
        string caption,
        string paramId,
        IBooleanValueGetter? visible = null,
        int parentIndex = -1)
    {
        const string arrayId = "CustomOperationPropertiesArray";
        using var propCom = _propHelpersCom.InvokeAndWrap(propHelpers => propHelpers.CreateComplexProp(caption));
        return propCom.Invoke(prop =>
        {
            prop.PropID = $"{arrayId}({paramId})";
            prop.Visible = visible ?? new BooleanValueGetter(() => true);
            return propIteratorCom.Invoke(iterator => iterator.AddNewProp(prop, parentIndex));
        });
    }

    public bool ReadBoolParam(ComWrapper<ICamApiTechOperation> operationCom, string paramId)
    {
        var operation = operationCom.Instance
                        ?? throw new Exception("OperationSolver container is not initialized");
        
        const string arrayId = "CustomOperationPropertiesArray";
        var fullId = $"{arrayId}({paramId})";
        return operation.XMLProp.PropExists[fullId] && 
               operation.XMLProp.Str[fullId].Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public string ReadEnumWithIdParam(ICamApiTechOperation operation, string paramId)
    {
        const string arrayId = "CustomOperationPropertiesArray";
        var fullId = $"{arrayId}({paramId})";
        return operation.XMLProp.PropExists[fullId] ? operation.XMLProp.Str[fullId] : "";
    }

    public double ReadDoubleParam(ICamApiTechOperation? operationComInstance, string paramId)
    {
        var operation = operationComInstance
                        ?? throw new Exception("OperationSolver container is not initialized");
        
        const string arrayId = "CustomOperationPropertiesArray";
        var fullId = $"{arrayId}({paramId})";
        return operation.XMLProp.PropExists[fullId] ? operation.XMLProp.Flt[fullId] : 0.0;
    }

    public int ReadIntegerParam(ICamApiTechOperation operation, string paramId)
    {
        const string arrayId = "CustomOperationPropertiesArray";
        var fullId = $"{arrayId}({paramId})";
        return operation.XMLProp.PropExists[fullId] ? operation.XMLProp.Int[fullId] : 0;
    }
}