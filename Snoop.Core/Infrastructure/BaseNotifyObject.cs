namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CA1045 // Do not pass types by reference
public class BaseNotifyObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, in T newValue, [CallerMemberName] string? propertyName = null)

    {
        if (propertyName == null)
        {
            return false;
        }

        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }

        field = newValue;
        this.RaisePropertyChanged(propertyName);
        return true;
    }

    protected bool Set<T>(Expression<Func<T>> propertyExpression, ref T field, in T newValue) => this.Set<T>(ref field, newValue, GetPropertyName(propertyExpression));

    /// <summary>
    /// Notify additional properties if the value is true, can be used like:
    /// set => ChangedIf(Set(ref _proxy_ip, value),()=>ProxyAddy,()=>proxy_port);
    /// </summary>
    /// <param name="val"></param>
    /// <param name="propertyExpressions"></param>
    /// <returns></returns>
    protected bool ChangedIf(bool val, params LambdaExpression[] propertyExpressions)
    {
        if (val && this.PropertyChanged != null)
        {
            foreach (var prop in propertyExpressions)
            {
                var name = GetPropertyName(prop);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    this.RaisePropertyChanged(name);
                }
            }
        }

        return val;
    }

    public virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression) => this.RaisePropertyChanged(GetPropertyName(propertyExpression));

    public virtual void RaisePropertyChanged([CallerMemberName] string propertyName = "") => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected static string GetPropertyName(LambdaExpression propertyExpression) => GetPropertyInfo(propertyExpression).Name;

    protected static PropertyInfo GetPropertyInfo(LambdaExpression propertyExpression)
    {
        if (propertyExpression == null)
        {
            throw new ArgumentNullException("propertyExpression");
        }

        var body = propertyExpression.Body as MemberExpression;

        if (body == null)
        {
            throw new ArgumentException("Invalid argument", "propertyExpression");
        }

        var property = body.Member as PropertyInfo;
        if (property == null)
        {
            throw new ArgumentException("Argument is not a property", "propertyExpression");
        }

        return property;
    }
}
#pragma warning restore CA1045 // Do not pass types by reference