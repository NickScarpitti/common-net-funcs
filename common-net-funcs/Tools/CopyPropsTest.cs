using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Common_Net_Funcs.Tools;

namespace Common_Net_Funcs.ToolsX.Experimental;
public static class DeepCloneExpressionTreeHelpers
{
    private static readonly object IsStructTypeToDeepCopyDictionaryLocker = new();
    private static Dictionary<Type, bool> IsStructTypeToDeepCopyDictionary = [];

    private static readonly object CompiledCopyFunctionsDictionaryLocker = new();
    private static Dictionary<(Type, Type), Func<object, Dictionary<object, object>, object>> CompiledCopyFunctionsDictionary = [];

    private static readonly Type ObjectType = typeof(object);
    private static readonly Type ObjectDictionaryType = typeof(Dictionary<object, object>);

    /// <summary>
    /// Deep clone a non-delegate type class (cloned object doesn't retain memory references) using Expression Trees (fastest)
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="original">Object to copy.</param>
    /// <param name="copiedReferencesDict">Dictionary of already copied objects (Keys: original objects, Values: their copies).</param>
    [return: NotNullIfNotNull(nameof(original))]
    public static UT? DeepCloneX<T, UT>(this T original, UT destination, Dictionary<object, object>? copiedReferencesDict = null)
    {
        return (UT?)DeepCopyByExpressionTreeObj(original, destination, false, copiedReferencesDict ?? new Dictionary<object, object>(new Tools.ReferenceEqualityComparer()));
    }

    private static object? DeepCopyByExpressionTreeObj(object? original, object? destination, bool forceDeepCopy, Dictionary<object, object> copiedReferencesDict)
    {
        if (original == null || destination == null)
        {
            return null;
        }

        Type type = original.GetType();
        Type outputType = destination.GetType();

        if (type == outputType) //If types are the same, short circuit to regular DeepClone
        {
            return original.DeepClone();
        }

        if (type.IsDelegate())
        {
            throw new ArgumentException($"Type {type.FullName} is a delegate type which is unsupported.", nameof(original));
            //return null;
        }

        if (outputType.IsDelegate())
        {
            throw new ArgumentException($"Output type {outputType.FullName} is a delegate type which is unsupported.", nameof(original));
            //return null;
        }

        if (!type.IsTypeToDeepCopy())
        {
            throw new ArgumentException($"Type {type.FullName} is not supported.", nameof(original));
            //return original;
        }

        if (!outputType.IsTypeToDeepCopy())
        {
            throw new ArgumentException($"Type {outputType.FullName} is not supported.", nameof(destination));
        }

        //if (copiedReferencesDict.TryGetValue(original, out object? alreadyCopiedObject))
        //{
        //    return alreadyCopiedObject;
        //}

        if (type == ObjectType)
        {
            return new();
        }

        Func<object, Dictionary<object, object>, object> compiledCopyFunction = GetOrCreateCompiledLambdaCopyFunction(type, outputType);
        return compiledCopyFunction(original, copiedReferencesDict);
    }

    private static Func<object, Dictionary<object, object>, object> GetOrCreateCompiledLambdaCopyFunction(Type type, Type outputType)
    {
        // The following structure ensures that multiple threads can use the dictionary even while dictionary is locked and being updated by other thread.
        // That is why we do not modify the old dictionary instance but we replace it with a new instance every time.
        if (!CompiledCopyFunctionsDictionary.TryGetValue((type, outputType), out Func<object, Dictionary<object, object>, object>? compiledCopyFunction))
        {
            lock (CompiledCopyFunctionsDictionaryLocker)
            {
                if (!CompiledCopyFunctionsDictionary.TryGetValue((type, outputType), out compiledCopyFunction))
                {
                    Expression<Func<object, Dictionary<object, object>, object>> uncompiledCopyFunction = CreateCompiledLambdaCopyFunctionForType(type); //TODO:: Continue from here
                    compiledCopyFunction = uncompiledCopyFunction.Compile();

                    Dictionary<(Type, Type), Func<object, Dictionary<object, object>, object>> dictionaryCopy = CompiledCopyFunctionsDictionary.ToDictionary(pair => pair.Key, pair => pair.Value);
                    dictionaryCopy.Add((type, outputType), compiledCopyFunction);
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
        if (type.IsClassOtherThanString())
        {
            StoreReferencesIntoDictionaryExpression(inputParameter, inputDictionary, outputVariable, expressions);
        }

        ///// COPY ALL NONVALUE OR NONPRIMITIVE FIELDS
        FieldsCopyExpressions(type, inputParameter, inputDictionary, outputVariable, boxingVariable, expressions);

        ///// COPY ELEMENTS OF ARRAY
        if (type.IsArray() && type.GetElementType().IsTypeToDeepCopy())
        {
            CreateArrayCopyLoopExpression(type, inputParameter, inputDictionary, outputVariable, variables, expressions);
        }

        ///// COMBINE ALL EXPRESSIONS INTO LAMBDA FUNCTION
        return CombineAllIntoLambdaFunctionExpression(inputParameter, inputDictionary, outputVariable, endLabel, variables, expressions);
    }

    private static void InitializeExpressions(Type type, out ParameterExpression inputParameter, out ParameterExpression inputDictionary, out ParameterExpression outputVariable,
        out ParameterExpression boxingVariable, out LabelTarget endLabel, out List<ParameterExpression> variables, out List<Expression> expressions)
    {
        inputParameter = Expression.Parameter(ObjectType);
        inputDictionary = Expression.Parameter(ObjectDictionaryType);
        outputVariable = Expression.Variable(type);
        boxingVariable = Expression.Variable(ObjectType);
        endLabel = Expression.Label();
        variables = [];
        expressions = [];

        variables.Add(outputVariable);
        variables.Add(boxingVariable);
    }

    private static void IfNullThenReturnNullExpression(ParameterExpression inputParameter, LabelTarget endLabel, List<Expression> expressions)
    {
        ///// Intended code:
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
        ///// var output = (<type>)input.MemberwiseClone();
        MethodInfo memberwiseCloneMethod = ObjectType.GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!;
        BinaryExpression memberwiseCloneInputExpression = Expression.Assign(outputVariable, Expression.Convert(Expression.Call(inputParameter, memberwiseCloneMethod), type));
        expressions.Add(memberwiseCloneInputExpression);
    }

    private static void StoreReferencesIntoDictionaryExpression(ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, List<Expression> expressions)
    {
        ///// Intended code:
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
        Expression forExpression = ArrayFieldToArrayFieldAssignExpression(inputParameter, inputDictionary, outputVariable, elementType, type, indices);

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
        ///// int i1, i2, ..., in;
        List<ParameterExpression> indices = [];
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
            Expression.Loop
            (
                Expression.Block
                (
#pragma warning disable IDE0301 // Simplify collection initialization
                    Array.Empty<ParameterExpression>(),
#pragma warning restore IDE0301 // Simplify collection initialization
                    Expression.IfThen(Expression.GreaterThanOrEqual(indexVariable, lengthVariable), Expression.Break(endLabelForThisLoop)),
                    loopToEncapsulate,
                    Expression.PostIncrementAssign(indexVariable)
                ),
                endLabelForThisLoop
            );

        BinaryExpression lengthAssignment = GetLengthForDimensionExpression(lengthVariable, inputParameter, dimension);
        BinaryExpression indexAssignment = Expression.Assign(indexVariable, Expression.Constant(0));
#pragma warning disable IDE0300 // Simplify collection initialization
        return Expression.Block(new[] { lengthVariable }, lengthAssignment, indexAssignment, newLoop);
#pragma warning restore IDE0300 // Simplify collection initialization
    }

    private static BinaryExpression GetLengthForDimensionExpression(ParameterExpression lengthVariable, ParameterExpression inputParameter, int i)
    {
        MethodInfo? getLengthMethod = typeof(Array).GetMethod("GetLength", BindingFlags.Public | BindingFlags.Instance);
        ConstantExpression dimensionConstant = Expression.Constant(i);
#pragma warning disable IDE0300 // Simplify collection initialization
        return Expression.Assign(lengthVariable, Expression.Call(Expression.Convert(inputParameter, typeof(Array)), getLengthMethod!, new[] { dimensionConstant }));
#pragma warning restore IDE0300 // Simplify collection initialization
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
            if (field.FieldType.IsDelegate())
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
        foreach (FieldInfo field in writableFields)
        {
            if (field.FieldType.IsDelegate())
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
        List<FieldInfo> fieldsList = [];
        Type? typeCache = type;
        while (typeCache != null)
        {
            fieldsList.AddRange(typeCache.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                    .Where(field => forceAllFields || field.FieldType.IsTypeToDeepCopy()));
            typeCache = typeCache.BaseType;
        }
        return fieldsList.ToArray();
    }

    private static FieldInfo[] GetAllFields(Type type)
    {
        return GetAllRelevantFields(type, forceAllFields: true);
    }

    private static readonly Type FieldInfoType = typeof(FieldInfo);
    private static readonly MethodInfo? SetValueMethod = FieldInfoType.GetMethod("SetValue", [ObjectType, ObjectType]);

    private static void ReadonlyFieldToNullExpression(FieldInfo field, ParameterExpression boxingVariable, List<Expression> expressions)
    {
        // This option must be implemented by Reflection because of the following:
        // https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2727812-allow-expression-assign-to-set-readonly-struct-f

        ///// Intended code:
        ///// fieldInfo.SetValue(boxing, <fieldtype>null);
        MethodCallExpression fieldToNullExpression = Expression.Call(Expression.Constant(field), SetValueMethod!, boxingVariable, Expression.Constant(null, field.FieldType));
        expressions.Add(fieldToNullExpression);
    }

    private static readonly Type ThisType = typeof(DeepCloneExpressionTreeHelpers);
    private static readonly MethodInfo? DeepCopyByExpressionTreeObjMethod = ThisType.GetMethod("DeepCopyByExpressionTreeObj", BindingFlags.NonPublic | BindingFlags.Static);

    private static void ReadonlyFieldCopyExpression(Type type, FieldInfo field, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression boxingVariable, List<Expression> expressions)
    {
        // This option must be implemented by Reflection (SetValueMethod) because of the following:
        // https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2727812-allow-expression-assign-to-set-readonly-struct-f

        ///// Intended code:
        ///// fieldInfo.SetValue(boxing, DeepCopyByExpressionTreeObj((Object)((<type>)input).<field>))

        MemberExpression fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);
        bool forceDeepCopy = field.FieldType != ObjectType;
        MethodCallExpression fieldDeepCopyExpression =
            Expression.Call
            (
                Expression.Constant(field, FieldInfoType),
                SetValueMethod!,
                boxingVariable,
                Expression.Call(DeepCopyByExpressionTreeObjMethod!, Expression.Convert(fieldFrom, ObjectType), Expression.Constant(forceDeepCopy, typeof(bool)), inputDictionary)
            );

        expressions.Add(fieldDeepCopyExpression);
    }

    private static void WritableFieldToNullExpression(FieldInfo field, ParameterExpression outputVariable, List<Expression> expressions)
    {
        ///// Intended code:
        ///// output.<field> = (<type>)null;
        MemberExpression fieldTo = Expression.Field(outputVariable, field);
        BinaryExpression fieldToNullExpression = Expression.Assign(fieldTo, Expression.Constant(null, field.FieldType));
        expressions.Add(fieldToNullExpression);
    }

    private static void WritableFieldCopyExpression(Type type, FieldInfo field, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, List<Expression> expressions)
    {
        ///// Intended code:
        ///// output.<field> = (<fieldType>)DeepCopyByExpressionTreeObj((Object)((<type>)input).<field>);
        MemberExpression fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);
        Type fieldType = field.FieldType;
        MemberExpression fieldTo = Expression.Field(outputVariable, field);

        bool forceDeepCopy = field.FieldType != ObjectType;

        BinaryExpression fieldDeepCopyExpression =
            Expression.Assign
            (
                fieldTo,
                Expression.Convert
                (
                    Expression.Call(DeepCopyByExpressionTreeObjMethod!, Expression.Convert(fieldFrom, ObjectType), Expression.Constant(forceDeepCopy, typeof(bool)), inputDictionary),
                    fieldType
                )
            );

        expressions.Add(fieldDeepCopyExpression);
    }

    private static bool IsTypeToDeepCopy(this Type? type)
    {
        return type.IsClassOtherThanString() || type.IsStructWhichNeedsDeepCopy();
    }

    private static bool IsStructWhichNeedsDeepCopy(this Type? type)
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
                    isStructTypeToDeepCopy = type!.IsStructWhichNeedsDeepCopy_NoDictionaryUsed();
                    Dictionary<Type, bool> newDictionary = IsStructTypeToDeepCopyDictionary.ToDictionary(pair => pair.Key, pair => pair.Value);
                    newDictionary[type!] = isStructTypeToDeepCopy;
                    IsStructTypeToDeepCopyDictionary = newDictionary;
                }
            }
        }

        return isStructTypeToDeepCopy;
    }

    private static bool IsStructWhichNeedsDeepCopy_NoDictionaryUsed(this Type type)
    {
        return type.IsStructOtherThanBasicValueTypes() && type.HasInItsHierarchyFieldsWithClasses();
    }

    private static bool IsStructOtherThanBasicValueTypes(this Type type)
    {
        return type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal);
    }

    private static bool HasInItsHierarchyFieldsWithClasses(this Type type, HashSet<Type>? alreadyCheckedTypes = null)
    {
        alreadyCheckedTypes ??= [];
        alreadyCheckedTypes.Add(type);

        FieldInfo[] allFields = GetAllFields(type);
        IEnumerable<Type> allFieldTypes = allFields.Select(f => f.FieldType).Distinct().ToList();

        bool hasFieldsWithClasses = allFieldTypes.Any(x => x.IsClassOtherThanString());
        if (hasFieldsWithClasses)
        {
            return true;
        }

        IEnumerable<Type> notBasicStructsTypes = allFieldTypes.Where(x => x.IsStructOtherThanBasicValueTypes()).ToList();
        foreach (Type typeToCheck in (IEnumerable<Type>)notBasicStructsTypes.Where(t => !alreadyCheckedTypes.Contains(t)).ToList())
        {
            if (typeToCheck.HasInItsHierarchyFieldsWithClasses(alreadyCheckedTypes))
            {
                return true;
            }
        }

        return false;
    }
}
