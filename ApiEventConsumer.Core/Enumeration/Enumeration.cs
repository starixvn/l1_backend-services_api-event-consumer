using System.Collections;
using System.Reflection;

namespace ApiEventConsumer.Core.Enumeration
{
    public class Enumeration : IComparable
    {
        protected Enumeration(string value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        public string DisplayName { get; set; }

        public string Value { get; set; }

        public static T FromDisplayName<T>(string displayName) where T : Enumeration, new()
        {
            var matchingItem = Parse<T, string>(displayName, "display name", item => item.DisplayName == displayName);
            return matchingItem;
        }

        public static T FromValue<T>(string value) where T : Enumeration, new()
        {
            var matchingItem = Parse<T, string>(value, "value", item => item.Value == value);
            return matchingItem;
        }

        public static bool Contains<T>(string value) where T : Enumeration, new()
        {
            try
            {
                var matchingItem = Parse<T, string>(value, "value", item => item.Value == value);
                return matchingItem is not null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static IEnumerable<T> GetAll<T>() where T : Enumeration, new()
        {
            var type = typeof(T);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (var info in fields)
            {
                var instance = new T();
                var locatedValue = info.GetValue(instance) as T;

                if (locatedValue is not null)
                {
                    yield return locatedValue;
                }
            }
        }

        public static IEnumerable GetAll(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (var info in fields)
            {
                var instance = Activator.CreateInstance(type);
                var value = info.GetValue(instance);
                if (value is not null)
                {
                    yield return value;
                }
            }
        }

        public static bool operator ==(Enumeration? left, Enumeration? right)
        {
            return left is null ? right is null : !(right is null) && left.Value == right.Value;
        }

        public static bool operator !=(Enumeration left, Enumeration right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            var otherValue = obj as Enumeration;
            if (otherValue is null)
            {
                return false;
            }

            var typeMatches = GetType().Equals(obj.GetType());
            var valueMatches = Value.Equals(otherValue.Value);

            return typeMatches && valueMatches;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public virtual int CompareTo(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return string.CompareOrdinal(Value, ((Enumeration)obj).Value);
        }

        protected static T Parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration, new()
        {
            var matchingItem = GetAll<T>().FirstOrDefault(predicate);

            if (matchingItem == null)
            {
                var message = string.Format("'{0}' is not a valid {1} in {2}", value, description, typeof(T));
                throw new ArgumentOutOfRangeException(message);
            }

            return matchingItem;
        }

        public static bool operator <(Enumeration? left, Enumeration? right)
        {
            if (left is null) return right is not null;
            return right is not null && left.CompareTo(right) < 0;
        }

        public static bool operator <=(Enumeration? left, Enumeration? right)
        {
            if (left is null) return true;
            return right is not null && left.CompareTo(right) <= 0;
        }

        public static bool operator >(Enumeration? left, Enumeration? right)
        {
            if (left is null) return false;
            return right is null || left.CompareTo(right) > 0;
        }

        public static bool operator >=(Enumeration? left, Enumeration? right)
        {
            if (right is null) return true;
            return left is not null && left.CompareTo(right) >= 0;
        }
    }
}
