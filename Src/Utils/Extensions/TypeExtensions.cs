using System.Text;

namespace Engine3.Utils.Extensions {
	public static class TypeExtensions {
		extension(Type self) {
			public string ToReadableName() {
				StringBuilder sb = new();
				VisitType(self, sb, self);
				return sb.ToString();

				static void VisitType(Type self2, StringBuilder sb, Type type) {
					if (type.IsArray) {
						Queue<string> rankDeclarations = new();
						Type elementType = type;

						do {
							rankDeclarations.Enqueue($"[{new string(',', elementType.GetArrayRank() - 1)}]");
							elementType = elementType.GetElementType()!; // should be checked above
						} while (elementType.IsArray);

						VisitType(self2, sb, elementType);

						while (rankDeclarations.Count > 0) { sb.Append(rankDeclarations.Dequeue()); }
					} else if (type.IsGenericType) {
						using IEnumerator<Type> genericArgsEnumerator = type.GetGenericArguments().AsEnumerable().GetEnumerator();
						genericArgsEnumerator.MoveNext();

						bool isNullable = self2.GetGenericTypeDefinition() == typeof(Nullable<>);
						if (!isNullable) { sb.Append($"{type.Name[..type.Name.IndexOf('`')]}<"); }

						VisitType(self2, sb, genericArgsEnumerator.Current);

						while (genericArgsEnumerator.MoveNext()) {
							sb.Append(',');
							VisitType(self2, sb, genericArgsEnumerator.Current);
						}

						sb.Append(isNullable ? '?' : '>');
					} else { sb.Append(type.Name); }
				}
			}
		}
	}
}