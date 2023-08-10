using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;

namespace Common_Net_Funcs.Tools;

public static class DeepCloneSerializationHelpers
{
    /// <summary>
    /// Deep clone a class (cloned object doesn't retain memory references) using reflection (slowest)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static IEnumerable<T>? SerializeClone<T>(this IEnumerable<T> list)
    {
        if (list == null) { return null; }
        string serialized = JsonConvert.SerializeObject(list);
        return JsonConvert.DeserializeObject<IEnumerable<T>>(serialized);
    }

    /// <summary>
    /// Deep clone a class (cloned object doesn't retain memory references) using serialization (slowest)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static List<T>? SerializeClone<T>(this IList<T> list)
    {
        if (list == null) { return null; }
        string serialized = JsonConvert.SerializeObject(list);
        return JsonConvert.DeserializeObject<List<T>>(serialized);
    }
}

public static class DeepCloneReflectionHelpers
{
    private static readonly MethodInfo? CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Deep clone a class (cloned object doesn't retain memory references) using reflection (mid-tier speed)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="original"></param>
    public static T? DeepCloneReflection<T>(this T? original)
    {
        return (T?)Copy(original);
    }

    public static bool IsPrimitive(this Type type)
    {
        if (type == typeof(string)) return true;
        return type.IsValueType && type.IsPrimitive;
    }

    private static object? Copy(this object? originalObject)
    {
        return InternalCopy(originalObject, new Dictionary<object, object?>(new ReferenceEqualityComparer()));
    }
    private static object? InternalCopy(object? originalObject, IDictionary<object, object?> visited)
    {
        if (originalObject == null) return null;
        Type typeToReflect = originalObject.GetType();
        if (IsPrimitive(typeToReflect)) return originalObject;
        if (visited.ContainsKey(originalObject)) return visited[originalObject];
        if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
        object? cloneObject = CloneMethod!.Invoke(originalObject, null);
        if (typeToReflect.IsArray)
        {
            Type? arrayType = typeToReflect.GetElementType();
            if (!IsPrimitive(arrayType!))
            {
                Array clonedArray = (Array)cloneObject!;
                clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
            }
        }
        visited.Add(originalObject, cloneObject);
        CopyFields(originalObject, visited, cloneObject, typeToReflect);
        RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
        return cloneObject;
    }

    private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object?> visited, object? cloneObject, Type typeToReflect)
    {
        if (typeToReflect.BaseType != null)
        {
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
            CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
        }
    }

    private static void CopyFields(object originalObject, IDictionary<object, object?> visited, object? cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool>? filter = null)
    {
        foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
        {
            if (filter != null && !filter(fieldInfo)) continue;
            if (IsPrimitive(fieldInfo.FieldType)) continue;
            var originalFieldValue = fieldInfo.GetValue(originalObject);
            var clonedFieldValue = InternalCopy(originalFieldValue, visited);
            fieldInfo.SetValue(cloneObject, clonedFieldValue);
        }
    }
}

public class ReferenceEqualityComparer : EqualityComparer<object>
{
    public override bool Equals(object? x, object? y)
    {
        return ReferenceEquals(x, y);
    }
    public override int GetHashCode(object obj)
    {
        if (obj == null) return 0;
        return obj.GetHashCode();
    }
}

public static class ArrayExtensions
{
    public static void ForEach(this Array array, Action<Array, int[]> action)
    {
        if (array.LongLength == 0) return;
        ArrayTraverse walker = new(array);
        do action(array, walker.Position);
        while (walker.Step());
    }
}

internal class ArrayTraverse
{
    public int[] Position;
    private readonly int[] maxLengths;

    public ArrayTraverse(Array array)
    {
        maxLengths = new int[array.Rank];
        for (int i = 0; i < array.Rank; ++i)
        {
            maxLengths[i] = array.GetLength(i) - 1;
        }
        Position = new int[array.Rank];
    }

    public bool Step()
    {
        for (int i = 0; i < Position.Length; ++i)
        {
            if (Position[i] < maxLengths[i])
            {
                Position[i]++;
                for (int j = 0; j < i; j++)
                {
                    Position[j] = 0;
                }
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Super fast deep copier class, which uses Expression trees.
/// </summary>
public static class DeepCloneExpressionTreeHelpers
{
    private static readonly object IsStructTypeToDeepCopyDictionaryLocker = new();
    private static Dictionary<Type, bool> IsStructTypeToDeepCopyDictionary = new();

    private static readonly object CompiledCopyFunctionsDictionaryLocker = new();
    private static Dictionary<Type, Func<object, Dictionary<object, object>, object>> CompiledCopyFunctionsDictionary = new();

    private static readonly Type ObjectType = typeof(Object);
    private static readonly Type ObjectDictionaryType = typeof(Dictionary<object, object>);

    /// <summary>
    /// Deep clone a class (cloned object doesn't retain memory references) using Expression Trees (fastest)
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="original">Object to copy.</param>
    /// <param name="copiedReferencesDict">Dictionary of already copied objects (Keys: original objects, Values: their copies).</param>
    public static T? DeepClone<T>(this T original, Dictionary<object, object>? copiedReferencesDict = null)
    {
        return (T?)DeepCopyByExpressionTreeObj(original, false, copiedReferencesDict ?? new Dictionary<object, object>(new ReferenceEqualityComparer()));
    }

    private static object? DeepCopyByExpressionTreeObj(object? original, bool forceDeepCopy, Dictionary<object, object> copiedReferencesDict)
    {
        if (original == null)
        {
            return null;
        }

        var type = original.GetType();

        if (IsDelegate(type))
        {
            return null;
        }

        if (!forceDeepCopy && !IsTypeToDeepCopy(type))
        {
            return original;
        }

        if (copiedReferencesDict.TryGetValue(original, out object? alreadyCopiedObject))
        {
            return alreadyCopiedObject;
        }

        if (type == ObjectType)
        {
            return new object();
        }

        var compiledCopyFunction = GetOrCreateCompiledLambdaCopyFunction(type);

        return compiledCopyFunction(original, copiedReferencesDict);
    }

    private static Func<object, Dictionary<object, object>, object> GetOrCreateCompiledLambdaCopyFunction(Type type)
    {
        // The following structure ensures that multiple threads can use the dictionary even while dictionary is locked and being updated by other thread.
        // That is why we do not modify the old dictionary instance but we replace it with a new instance every time.

        if (!CompiledCopyFunctionsDictionary.TryGetValue(type, out Func<object, Dictionary<object, object>, object>? compiledCopyFunction))
        {
            lock (CompiledCopyFunctionsDictionaryLocker)
            {
                if (!CompiledCopyFunctionsDictionary.TryGetValue(type, out compiledCopyFunction))
                {
                    Expression<Func<object, Dictionary<object, object>, object>> uncompiledCopyFunction = CreateCompiledLambdaCopyFunctionForType(type);

                    compiledCopyFunction = uncompiledCopyFunction.Compile();

                    var dictionaryCopy = CompiledCopyFunctionsDictionary.ToDictionary(pair => pair.Key, pair => pair.Value);

                    dictionaryCopy.Add(type, compiledCopyFunction);

                    CompiledCopyFunctionsDictionary = dictionaryCopy;
                }
            }
        }

        return compiledCopyFunction;
    }

    private static Expression<Func<object, Dictionary<object, object>, object>> CreateCompiledLambdaCopyFunctionForType(Type type)
    {
        ///// INITIALIZATION OF EXPRESSIONS AND VARIABLES
        InitializeExpressions(type, out ParameterExpression inputParameter, out ParameterExpression inputDictionary, out ParameterExpression outputVariable, out ParameterExpression boxingVariable,
                out LabelTarget endLabel, out List<ParameterExpression> variables, out List<Expression> expressions);

        ///// RETURN NULL IF ORIGINAL IS NULL
        IfNullThenReturnNullExpression(inputParameter, endLabel, expressions);

        ///// MEMBERWISE CLONE ORIGINAL OBJECT
        MemberwiseCloneInputToOutputExpression(type, inputParameter, outputVariable, expressions);

        ///// STORE COPIED OBJECT TO REFERENCES DICTIONARY
        if (IsClassOtherThanString(type))
        {
            StoreReferencesIntoDictionaryExpression(inputParameter, inputDictionary, outputVariable, expressions);
        }

        ///// COPY ALL NONVALUE OR NONPRIMITIVE FIELDS
        FieldsCopyExpressions(type, inputParameter, inputDictionary, outputVariable, boxingVariable, expressions);

        ///// COPY ELEMENTS OF ARRAY
        if (IsArray(type) && IsTypeToDeepCopy(type.GetElementType()))
        {
            CreateArrayCopyLoopExpression(type, inputParameter, inputDictionary, outputVariable, variables, expressions);
        }

        ///// COMBINE ALL EXPRESSIONS INTO LAMBDA FUNCTION
        Expression<Func<object, Dictionary<object, object>, object>> lambda = CombineAllIntoLambdaFunctionExpression(inputParameter, inputDictionary, outputVariable, endLabel, variables, expressions);

        return lambda;
    }

    private static void InitializeExpressions(Type type, out ParameterExpression inputParameter, out ParameterExpression inputDictionary, out ParameterExpression outputVariable,
        out ParameterExpression boxingVariable, out LabelTarget endLabel, out List<ParameterExpression> variables, out List<Expression> expressions)
    {
        inputParameter = Expression.Parameter(ObjectType);

        inputDictionary = Expression.Parameter(ObjectDictionaryType);

        outputVariable = Expression.Variable(type);

        boxingVariable = Expression.Variable(ObjectType);

        endLabel = Expression.Label();

        variables = new List<ParameterExpression>();

        expressions = new List<Expression>();

        variables.Add(outputVariable);
        variables.Add(boxingVariable);
    }

    private static void IfNullThenReturnNullExpression(ParameterExpression inputParameter, LabelTarget endLabel, List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// if (input == null)
        ///// {
        /////     return null;
        ///// }

        ConditionalExpression ifNullThenReturnNullExpression = Expression.IfThen(Expression.Equal(inputParameter, Expression.Constant(null, ObjectType)), Expression.Return(endLabel));

        expressions.Add(ifNullThenReturnNullExpression);
    }

    private static void MemberwiseCloneInputToOutputExpression(Type type, ParameterExpression inputParameter, ParameterExpression outputVariable, List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// var output = (<type>)input.MemberwiseClone();

        MethodInfo memberwiseCloneMethod = ObjectType.GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!;

        BinaryExpression memberwiseCloneInputExpression = Expression.Assign(outputVariable, Expression.Convert(Expression.Call(inputParameter, memberwiseCloneMethod), type));

        expressions.Add(memberwiseCloneInputExpression);
    }

    private static void StoreReferencesIntoDictionaryExpression(ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// inputDictionary[(Object)input] = (Object)output;

        BinaryExpression storeReferencesExpression = Expression.Assign(Expression.Property(inputDictionary, ObjectDictionaryType.GetProperty("Item")!, inputParameter), Expression.Convert(outputVariable, ObjectType));

        expressions.Add(storeReferencesExpression);
    }

    private static Expression<Func<object, Dictionary<object, object>, object>> CombineAllIntoLambdaFunctionExpression(ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable,
        LabelTarget endLabel, List<ParameterExpression> variables, List<Expression> expressions)
    {
        expressions.Add(Expression.Label(endLabel));

        expressions.Add(Expression.Convert(outputVariable, ObjectType));

        BlockExpression finalBody = Expression.Block(variables, expressions);

        return Expression.Lambda<Func<object, Dictionary<object, object>, object>>(finalBody, inputParameter, inputDictionary);
    }

    private static void CreateArrayCopyLoopExpression(Type type, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, List<ParameterExpression> variables, List<Expression> expressions)
    {
        int rank = type.GetArrayRank();

        List<ParameterExpression> indices = GenerateIndices(rank);

        variables.AddRange(indices);

        Type? elementType = type.GetElementType();

        BinaryExpression assignExpression = ArrayFieldToArrayFieldAssignExpression(inputParameter, inputDictionary, outputVariable, elementType, type, indices);

        Expression forExpression = assignExpression;

        for (int dimension = 0; dimension < rank; dimension++)
        {
            ParameterExpression indexVariable = indices[dimension];

            forExpression = LoopIntoLoopExpression(inputParameter, indexVariable, forExpression, dimension);
        }

        expressions.Add(forExpression);
    }

    private static List<ParameterExpression> GenerateIndices(int arrayRank)
    {
        ///// Intended code:
        /////
        ///// int i1, i2, ..., in;

        List<ParameterExpression> indices = new();

        for (int i = 0; i < arrayRank; i++)
        {
            ParameterExpression indexVariable = Expression.Variable(typeof(int));
            indices.Add(indexVariable);
        }

        return indices;
    }

    private static BinaryExpression ArrayFieldToArrayFieldAssignExpression(ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, Type? elementType, Type arrayType, List<ParameterExpression> indices)
    {
        IndexExpression indexTo = Expression.ArrayAccess(outputVariable, indices);

        MethodCallExpression indexFrom = Expression.ArrayIndex(Expression.Convert(inputParameter, arrayType), indices);

        bool forceDeepCopy = elementType != ObjectType;

        UnaryExpression rightSide = Expression.Convert(Expression.Call(DeepCopyByExpressionTreeObjMethod!, Expression.Convert(indexFrom, ObjectType), Expression.Constant(forceDeepCopy, typeof(bool)), inputDictionary), elementType!);

        return Expression.Assign(indexTo, rightSide);
    }

    private static BlockExpression LoopIntoLoopExpression(ParameterExpression inputParameter, ParameterExpression indexVariable, Expression loopToEncapsulate, int dimension)
    {
        ParameterExpression lengthVariable = Expression.Variable(typeof(int));

        LabelTarget endLabelForThisLoop = Expression.Label();

        LoopExpression newLoop =
            Expression.Loop(
                Expression.Block(
                    Array.Empty<ParameterExpression>(),
                    Expression.IfThen(
                        Expression.GreaterThanOrEqual(indexVariable, lengthVariable),
                        Expression.Break(endLabelForThisLoop)),
                    loopToEncapsulate,
                    Expression.PostIncrementAssign(indexVariable)),
                endLabelForThisLoop);

        BinaryExpression lengthAssignment = GetLengthForDimensionExpression(lengthVariable, inputParameter, dimension);

        BinaryExpression indexAssignment = Expression.Assign(indexVariable, Expression.Constant(0));

        return Expression.Block(new[] { lengthVariable }, lengthAssignment, indexAssignment, newLoop);
    }

    private static BinaryExpression GetLengthForDimensionExpression(ParameterExpression lengthVariable, ParameterExpression inputParameter, int i)
    {
        MethodInfo? getLengthMethod = typeof(Array).GetMethod("GetLength", BindingFlags.Public | BindingFlags.Instance);

        ConstantExpression dimensionConstant = Expression.Constant(i);

        return Expression.Assign(lengthVariable, Expression.Call(Expression.Convert(inputParameter, typeof(Array)), getLengthMethod!, new[] { dimensionConstant }));
    }

    private static void FieldsCopyExpressions(Type type, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, ParameterExpression boxingVariable, List<Expression> expressions)
    {
        FieldInfo[] fields = GetAllRelevantFields(type);

        IEnumerable<FieldInfo> readonlyFields = fields.Where(f => f.IsInitOnly).ToList();
        IEnumerable<FieldInfo> writableFields = fields.Where(f => !f.IsInitOnly).ToList();

        ///// READONLY FIELDS COPY (with boxing)

        bool shouldUseBoxing = readonlyFields.Any();

        if (shouldUseBoxing)
        {
            BinaryExpression boxingExpression = Expression.Assign(boxingVariable, Expression.Convert(outputVariable, ObjectType));

            expressions.Add(boxingExpression);
        }

        foreach (FieldInfo field in readonlyFields)
        {
            if (IsDelegate(field.FieldType))
            {
                ReadonlyFieldToNullExpression(field, boxingVariable, expressions);
            }
            else
            {
                ReadonlyFieldCopyExpression(type, field, inputParameter, inputDictionary, boxingVariable, expressions);
            }
        }

        if (shouldUseBoxing)
        {
            BinaryExpression unboxingExpression = Expression.Assign(outputVariable, Expression.Convert(boxingVariable, type));

            expressions.Add(unboxingExpression);
        }

        ///// NOT-READONLY FIELDS COPY

        foreach (var field in writableFields)
        {
            if (IsDelegate(field.FieldType))
            {
                WritableFieldToNullExpression(field, outputVariable, expressions);
            }
            else
            {
                WritableFieldCopyExpression(type, field, inputParameter, inputDictionary, outputVariable, expressions);
            }
        }
    }

    private static FieldInfo[] GetAllRelevantFields(Type? type, bool forceAllFields = false)
    {
        List<FieldInfo> fieldsList = new();

        Type? typeCache = type;

        while (typeCache != null)
        {
            fieldsList.AddRange(typeCache.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                    .Where(field => forceAllFields || IsTypeToDeepCopy(field.FieldType)));

            typeCache = typeCache.BaseType;
        }

        return fieldsList.ToArray();
    }

    private static FieldInfo[] GetAllFields(Type type)
    {
        return GetAllRelevantFields(type, forceAllFields: true);
    }

    private static readonly Type FieldInfoType = typeof(FieldInfo);
    private static readonly MethodInfo? SetValueMethod = FieldInfoType.GetMethod("SetValue", new[] { ObjectType, ObjectType });

    private static void ReadonlyFieldToNullExpression(FieldInfo field, ParameterExpression boxingVariable, List<Expression> expressions)
    {
        // This option must be implemented by Reflection because of the following:
        // https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2727812-allow-expression-assign-to-set-readonly-struct-f

        ///// Intended code:
        /////
        ///// fieldInfo.SetValue(boxing, <fieldtype>null);

        var fieldToNullExpression = Expression.Call(Expression.Constant(field), SetValueMethod!, boxingVariable, Expression.Constant(null, field.FieldType));

        expressions.Add(fieldToNullExpression);
    }

    private static readonly Type ThisType = typeof(DeepCloneExpressionTreeHelpers);
    private static readonly MethodInfo? DeepCopyByExpressionTreeObjMethod = ThisType.GetMethod("DeepCopyByExpressionTreeObj", BindingFlags.NonPublic | BindingFlags.Static);

    private static void ReadonlyFieldCopyExpression(Type type, FieldInfo field, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression boxingVariable, List<Expression> expressions)
    {
        // This option must be implemented by Reflection (SetValueMethod) because of the following:
        // https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2727812-allow-expression-assign-to-set-readonly-struct-f

        ///// Intended code:
        /////
        ///// fieldInfo.SetValue(boxing, DeepCopyByExpressionTreeObj((Object)((<type>)input).<field>))

        MemberExpression fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);

        bool forceDeepCopy = field.FieldType != ObjectType;

        MethodCallExpression fieldDeepCopyExpression =
            Expression.Call(
                Expression.Constant(field, FieldInfoType),
                SetValueMethod!,
                boxingVariable,
                Expression.Call(
                    DeepCopyByExpressionTreeObjMethod!,
                    Expression.Convert(fieldFrom, ObjectType),
                    Expression.Constant(forceDeepCopy, typeof(bool)),
                    inputDictionary));

        expressions.Add(fieldDeepCopyExpression);
    }

    private static void WritableFieldToNullExpression(FieldInfo field, ParameterExpression outputVariable, List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// output.<field> = (<type>)null;

        MemberExpression fieldTo = Expression.Field(outputVariable, field);

        var fieldToNullExpression = Expression.Assign(fieldTo, Expression.Constant(null, field.FieldType));

        expressions.Add(fieldToNullExpression);
    }

    private static void WritableFieldCopyExpression(Type type, FieldInfo field, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, List<Expression> expressions)
    {
        ///// Intended code:
        /////
        ///// output.<field> = (<fieldType>)DeepCopyByExpressionTreeObj((Object)((<type>)input).<field>);

        MemberExpression fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);

        Type fieldType = field.FieldType;

        MemberExpression fieldTo = Expression.Field(outputVariable, field);

        bool forceDeepCopy = field.FieldType != ObjectType;

        BinaryExpression fieldDeepCopyExpression =
            Expression.Assign(
                fieldTo,
                Expression.Convert(
                    Expression.Call(
                        DeepCopyByExpressionTreeObjMethod!,
                        Expression.Convert(fieldFrom, ObjectType),
                        Expression.Constant(forceDeepCopy, typeof(bool)),
                        inputDictionary),
                    fieldType));

        expressions.Add(fieldDeepCopyExpression);
    }

    private static bool IsArray(Type type)
    {
        return type.IsArray;
    }

    private static bool IsDelegate(Type type)
    {
        return typeof(Delegate).IsAssignableFrom(type);
    }

    private static bool IsTypeToDeepCopy(Type? type)
    {
        return IsClassOtherThanString(type)
               || IsStructWhichNeedsDeepCopy(type);
    }

    private static bool IsClassOtherThanString(Type? type)
    {
        return type == null || (!type.IsValueType && type != typeof(string)); //Added type == null || - Nick
    }

    private static bool IsStructWhichNeedsDeepCopy(Type? type)
    {
        // The following structure ensures that multiple threads can use the dictionary
        // even while dictionary is locked and being updated by other thread.
        // That is why we do not modify the old dictionary instance but
        // we replace it with a new instance every time.

        if (!IsStructTypeToDeepCopyDictionary.TryGetValue(type!, out bool isStructTypeToDeepCopy))
        {
            lock (IsStructTypeToDeepCopyDictionaryLocker)
            {
                if (!IsStructTypeToDeepCopyDictionary.TryGetValue(type!, out isStructTypeToDeepCopy))
                {
                    isStructTypeToDeepCopy = IsStructWhichNeedsDeepCopy_NoDictionaryUsed(type!);

                    var newDictionary = IsStructTypeToDeepCopyDictionary.ToDictionary(pair => pair.Key, pair => pair.Value);

                    newDictionary[type!] = isStructTypeToDeepCopy;

                    IsStructTypeToDeepCopyDictionary = newDictionary;
                }
            }
        }

        return isStructTypeToDeepCopy;
    }

    private static bool IsStructWhichNeedsDeepCopy_NoDictionaryUsed(Type type)
    {
        return IsStructOtherThanBasicValueTypes(type) && HasInItsHierarchyFieldsWithClasses(type);
    }

    private static bool IsStructOtherThanBasicValueTypes(Type type)
    {
        return type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal);
    }

    private static bool HasInItsHierarchyFieldsWithClasses(Type type, HashSet<Type>? alreadyCheckedTypes = null)
    {
        alreadyCheckedTypes ??= new HashSet<Type>();

        alreadyCheckedTypes.Add(type);

        FieldInfo[] allFields = GetAllFields(type);

        IEnumerable<Type> allFieldTypes = allFields.Select(f => f.FieldType).Distinct().ToList();

        bool hasFieldsWithClasses = allFieldTypes.Any(IsClassOtherThanString);

        if (hasFieldsWithClasses)
        {
            return true;
        }

        IEnumerable<Type> notBasicStructsTypes = allFieldTypes.Where(IsStructOtherThanBasicValueTypes).ToList();

        IEnumerable<Type> typesToCheck = notBasicStructsTypes.Where(t => !alreadyCheckedTypes.Contains(t)).ToList();

        foreach (var typeToCheck in typesToCheck)
        {
            if (HasInItsHierarchyFieldsWithClasses(typeToCheck, alreadyCheckedTypes))
            {
                return true;
            }
        }

        return false;
    }

    public class ReferenceEqualityComparer : EqualityComparer<object>
    {
        public override bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;

            return obj.GetHashCode();
        }
    }
}
