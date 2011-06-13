using System.Collections.Generic;
using System.Linq;
using NHibernate.Engine;
using NHibernate.SqlCommand;
using NHibernate.SqlTypes;
using NHibernate.Type;

namespace NHibernate.Param
{
	public static class ParametersBackTrackExtensions
	{
		public static IEnumerable<int> GetEffectiveParameterLocations(this IList<Parameter> sqlParameters, string backTrackId)
		{
			for (int i = 0; i < sqlParameters.Count; i++)
			{
				if (backTrackId.Equals(sqlParameters[i].BackTrack))
				{
					yield return i;
				}
			}
		}

		public static SqlType[] GetQueryParameterTypes(this IEnumerable<IParameterSpecification> parameterSpecs, List<Parameter> sqlQueryParametersList, ISessionFactoryImplementor factory)
		{
			// NOTE: if you have a NullReferenceException probably is because the IParameterSpecification does not have the ExpectedType; use ResetEffectiveExpectedType before call this method.
			
			// due to IType.NullSafeSet(System.Data.IDbCommand , object, int, ISessionImplementor) the SqlType[] is supposed to be in a certain sequence.
			// here we can check and evetually Assert (see AssertionFailure) the supposition because each individual Parameter has its BackTrackId.
			// Currently we just take the first BackTrackId of each IParameterSpecification
			IEnumerable<IType> typesSequence = from typeLocation in
			                                   	from specification in parameterSpecs
			                                   	let firstParameterId = specification.GetIdsForBackTrack(factory).First()
			                                   	let effectiveParameterLocations = sqlQueryParametersList.GetEffectiveParameterLocations(firstParameterId)
			                                   	from location in effectiveParameterLocations
			                                   	select new {Location = location, Type = specification.ExpectedType}
			                                   group typeLocation by typeLocation.Location
			                                   into locations
			                                   orderby locations.Key
			                                   select locations.First().Type;
			return typesSequence.SelectMany(t => t.SqlTypes(factory)).ToArray();
		}

		public static void ResetEffectiveExpectedType(this IEnumerable<IParameterSpecification> parameterSpecs, QueryParameters queryParameters)
		{
			// TODO: remove this method when we can infer the type during the parse
			foreach (var parameterSpecification in parameterSpecs.OfType<IExplicitParameterSpecification>())
			{
				parameterSpecification.SetEffectiveType(queryParameters);
			}
		}

		/// <summary>
		/// Influence the final name of the parameter.
		/// </summary>
		/// <param name="parameterSpecs"></param>
		/// <param name="sqlQueryParametersList"></param>
		/// <param name="factory"></param>
		public static void SetQueryParameterLocations(this IEnumerable<IParameterSpecification> parameterSpecs, List<Parameter> sqlQueryParametersList, ISessionFactoryImplementor factory)
		{
			// due to IType.NullSafeSet(System.Data.IDbCommand , object, int, ISessionImplementor) the SqlType[] is supposed to be in a certain sequence.
			// this mean that found the first location of a parameter for the IType span, the others are in secuence
			foreach (IParameterSpecification specification in parameterSpecs)
			{
				string firstParameterId = specification.GetIdsForBackTrack(factory).First();
				int[] effectiveParameterLocations = sqlQueryParametersList.GetEffectiveParameterLocations(firstParameterId).ToArray();
				if (effectiveParameterLocations.Length > 0) // Parameters previously present might have been removed from the SQL at a later point.
				{
					int firstParamNameIndex = effectiveParameterLocations.First();
					foreach (int location in effectiveParameterLocations)
					{
						int parameterSpan = specification.ExpectedType.GetColumnSpan(factory);
						for (int j = 0; j < parameterSpan; j++)
						{
							sqlQueryParametersList[location + j].ParameterPosition = firstParamNameIndex + j;
						}
					}
				}
			}
		}
	}
}