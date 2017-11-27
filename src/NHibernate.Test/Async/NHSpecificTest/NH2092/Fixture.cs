﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH2092
{
	using System.Threading.Tasks;
	[TestFixture]
	public class FixtureAsync : BugTestCase
	{
		[Test]
		public async Task ConstrainedLazyLoadedOneToOneUsingCastleProxyAsync()
		{
			try
			{
				using (var s = OpenSession())
				{
					var person = new Person { Id = 1, Name = "Person1" };
					var employee = new Employee { Id = 1, Name = "Emp1", Person = person };
					
					await (s.SaveAsync(person));
					await (s.SaveAsync(employee));

					await (s.FlushAsync());
				}

				using (var s = OpenSession())
				{
					var employee = await (s.GetAsync<Employee>(1));

					Assert.That(NHibernateUtil.IsInitialized(employee.Person), Is.False);

					Assert.That("Person1", Is.EqualTo(employee.Person.Name));

					Assert.That(NHibernateUtil.IsInitialized(employee.Person), Is.True);
				}
			}
			finally
			{
				using (var s = OpenSession())
				{
					await (s.DeleteAsync("from Employee"));
					await (s.DeleteAsync("from Person"));

					await (s.FlushAsync());
				}
			}
		}
	}
}