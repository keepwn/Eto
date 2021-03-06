using System;
using System.IO;
using System.Reflection;
using Eto.Forms;
using Eto.Threading;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using NUnitLite.Runner;
using NUnit.Framework.Api;
using System.Threading.Tasks;
using Eto.Drawing;
using System.Linq;
using Eto.Test.UnitTests.Handlers;
using NUnit;

namespace Eto.Test.Sections
{
	public class TestListener : ITestListener
	{
		public Application Application { get; set; }

		public void TestFinished(ITestResult result)
		{
			if (!result.HasChildren)
			{
				if (result.FailCount > 0)
				{
					Application.Invoke(() => Log.Write(null, "Failed: {0}\n{1}", result.Message, result.StackTrace));
				}
			}
		}

		public void TestOutput(TestOutput testOutput)
		{
			Application.Instance.Invoke(() => Log.Write(null, testOutput.Text));
		}

		public void TestStarted(ITest test)
		{
			if (!test.HasChildren)
				Application.Invoke(() => Log.Write(null, test.FullName));
		}
	}

	class SingleTestFilter : CategoryFilter
	{
		public ITest Test { get; set; }

		public override bool Pass(ITest test)
		{
			var parent = Test;
			// check if it is a parent of the test
			while (parent != null)
			{
				if (test.FullName == parent.FullName)
					return base.Pass(test);
				parent = parent.Parent;
			}
			// execute all children of the test
			while (test != null)
			{
				if (test.FullName == Test.FullName)
					return base.Pass(test);
				test = test.Parent;
			}
			return false;
		}
	}

	class NamespaceTestFilter : CategoryFilter
	{
		public string Namespace { get; set; }

		public override bool Pass(ITest test)
		{
			if (test.FixtureType == null && test.Parent != null)
				return Pass(test.Parent);
			var ns = test.FixtureType.Namespace;
			var pass = ns == Namespace || ns.StartsWith(Namespace + ".", StringComparison.Ordinal);
			return pass && base.Pass(test);
		}
	}

	class CategoryFilter : ITestFilter
	{
		public Application Application { get; set; }

		public List<string> IncludeCategories { get; private set; }

		public List<string> ExcludeCategories { get; private set; }

		public CategoryFilter()
		{
			IncludeCategories = new List<string>();
			ExcludeCategories = new List<string>();
		}

		public virtual bool Pass(ITest test)
		{
			var categoryList = test.Properties["Category"] as ObjectList;

			bool pass = true;
			if (categoryList != null)
			{
				var categories = categoryList.OfType<string>().ToList();
				if (IncludeCategories.Count > 0)
					pass = categories.Any(IncludeCategories.Contains);

				if (ExcludeCategories.Count > 0)
					pass &= !categories.Any(ExcludeCategories.Contains);
			}
			if (!pass)
				Application.Invoke(() => Log.Write(null, "Skipping {0} (excluded)", test.FullName));
			return pass;
		}

		public virtual bool IsEmpty { get { return false; } }
	}

	[Section("Tests", "Unit Tests")]
	public class UnitTestSection : Panel
	{
		TreeView tree;
		Button startButton;
		CheckBox useTestPlatform;

		public UnitTestSection()
		{
			startButton = new Button { Text = "Start Tests", Size = new Size(200, 80) };
			useTestPlatform = new CheckBox { Text = "Use Test Platform" };
			var buttons = new TableLayout(
				              TableLayout.AutoSized(startButton, centered: true),
				              TableLayout.AutoSized(useTestPlatform, centered: true)
			              );

			if (Platform.Supports<TreeView>())
			{
				tree = new TreeView();

				tree.Activated += (sender, e) =>
				{
					var item = (TreeItem)tree.SelectedItem;
					if (item != null)
					{
						RunTests(item.Tag as CategoryFilter);
					}
				};

				Content = new TableLayout(
					buttons,
					tree
				);
			}
			else
				Content = new TableLayout(null, buttons, null);

			startButton.Click += (s, e) => RunTests();
		}

		async void RunTests(CategoryFilter filter = null)
		{
			if (!startButton.Enabled)
				return;
			startButton.Enabled = false;
			Log.Write(null, "Starting tests...");
			var testPlatform = useTestPlatform.Checked == true ? new TestPlatform() : Platform;
			try
			{
				await Task.Run(() =>
				{
					using (Platform.ThreadStart())
					{
						try
						{
							var assembly = GetType().GetTypeInfo().Assembly;
							var runner = new NUnitLiteTestAssemblyRunner(new NUnitLiteTestAssemblyBuilder());
							if (!runner.Load(assembly, new Dictionary<string, object>()))
							{
								Log.Write(null, "Failed to load test assembly");
								return;
							}
							ITestResult result;
							var listener = new TestListener { Application = Application.Instance }; // use running application for logging
							filter = filter ?? new CategoryFilter();
							filter.Application = Application.Instance;
							if (testPlatform is TestPlatform)
							{
								filter.ExcludeCategories.Add(UnitTests.TestUtils.NoTestPlatformCategory);
							}
							using (testPlatform.Context)
							{
								result = runner.Run(listener, filter);
							}
							var writer = new StringWriter();
							writer.WriteLine(result.FailCount > 0 ? "FAILED" : "PASSED");
							writer.WriteLine("\tPass: {0}, Fail: {1}, Skipped: {2}, Inconclusive: {3}", result.PassCount, result.FailCount, result.SkipCount, result.InconclusiveCount);
							writer.Write("\tDuration: {0}", result.Duration);
							Application.Instance.Invoke(() => Log.Write(null, writer.ToString()));
						}
						catch (Exception ex)
						{
							Application.Instance.Invoke(() => Log.Write(null, "Error running tests: {0}", ex));
						}
						finally
						{
							Application.Instance.Invoke(() => startButton.Enabled = true);
						}
					}
				});
			}
			catch (Exception ex)
			{
				Log.Write(null, "Error running tests\n{0}", ex);
			}
		}

		protected override async void OnLoadComplete(EventArgs e)
		{
			base.OnLoadComplete(e);
			try
			{
				if (tree != null)
					await Task.Factory.StartNew(PopulateTree);
			}
			catch (Exception ex)
			{
				Log.Write(null, "Error populating tree\n{0}", ex);
			}
		}

		TestSuite GetTestSuite()
		{
			var assembly = GetType().GetTypeInfo().Assembly;
			var builder = new NUnitLiteTestAssemblyBuilder();
			return builder.Build(assembly, new Dictionary<string, object>());
		}

		void PopulateTree()
		{
			var testSuite = GetTestSuite();
			var treeData = ToTree(testSuite);
			Application.Instance.AsyncInvoke(() => tree.DataStore = treeData);
		}

		TreeItem ToTree(ITest test, int startIndex = 0)
		{
			// add a test
			var item = new TreeItem { Text = test.Name, Tag = new SingleTestFilter { Test = test } };
			if (test.HasChildren)
			{
				item.Children.AddRange(ToTree(test.Tests, startIndex));
			}
			return item;
		}

		IEnumerable<TreeItem> ToTree(IEnumerable<ITest> tests, int startIndex)
		{
			if (startIndex == -1)
			{
				foreach (var test in tests)
				{
					yield return ToTree(test, -1);
				}
				yield break;
			}

			// split namespaces and group them to the startIndex
			var namespaces = from s in
			                     (
			                         from r in tests
			                         group r by r.FixtureType.Namespace into g
			                         orderby g.Key
			                         select g.Key.Split('.').Take(startIndex + 1)
			                     )
			                 group s by s.Last() into gg // group by last namespace
			                 select gg.First();

			foreach (var ns in namespaces)
			{
				var fullns = string.Join(".", ns);
				var childtests = ToTree(from t in tests
				                        where t.FixtureType.Namespace.StartsWith(fullns + ".", StringComparison.Ordinal)
				                        select t, startIndex + 1);

				var nstests = ToTree(from t in tests
				                     where t.FixtureType.Namespace == fullns
				                     select t, -1);

				// add a namespace
				var ti = new TreeItem
				{
					Text = ns.Last(),
					Expanded = true,
					Tag = new NamespaceTestFilter { Namespace = fullns }
				};
				ti.Children.AddRange(from t in nstests.Concat(childtests)
				                     orderby t.Text
				                     select t);
				yield return ti;
			}
		}
	}
}