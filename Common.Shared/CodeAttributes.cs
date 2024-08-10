using System;

namespace Common;


[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class BusinessCodeAttribute : Attribute
{
    /// <summary><inheritdoc cref="BusinessCodeAttribute"/></summary>
    public BusinessCodeAttribute() { }
    /// <summary><inheritdoc cref="BusinessCodeAttribute"/></summary>
    public BusinessCodeAttribute(string desc) { }
}


[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class CodeNotCompletedTodoLaterAttribute : Attribute
{
    /// <summary><inheritdoc cref="CodeNotCompletedTodoLaterAttribute"/></summary>
    public CodeNotCompletedTodoLaterAttribute() { }
    /// <summary><inheritdoc cref="CodeNotCompletedTodoLaterAttribute"/></summary>
    public CodeNotCompletedTodoLaterAttribute(string desc) { }
}
