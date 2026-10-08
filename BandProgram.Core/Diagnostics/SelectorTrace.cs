using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BandProgram
{
	// Util.findElement* 가 요소를 못 찾았을 때 셀렉터와 호출 위치(Util 바깥 첫 프레임)를 기록한다.
	// 기록만 하고 반환값·예외 흐름은 바꾸지 않는다.
	public static class SelectorTrace
	{
		public static Action<string> Sink;

		public static bool SnapshotEnabled;

		internal static Func<DateTime> Now = () => DateTime.Now;

		internal static readonly TimeSpan DedupeWindow = TimeSpan.FromSeconds(60);

		private static readonly object sync = new object();

		private static readonly Dictionary<string, DateTime> lastLogged = new Dictionary<string, DateTime>();

		public static void Miss(string selector, IWebDriver driver)
		{
			Action<string> sink = Sink;
			if (sink == null)
			{
				return;
			}
			try
			{
				string caller = FindCaller(new StackTrace(1, true));
				DateTime now = Now();
				string key = string.Concat(selector, "|", caller);
				lock (sync)
				{
					DateTime last;
					if (lastLogged.TryGetValue(key, out last) && now - last < DedupeWindow)
					{
						return;
					}
					lastLogged[key] = now;
				}
				string snapshot = SnapshotEnabled ? SaveSnapshot(driver) : null;
				string text = string.Concat(
					"[SELECTOR MISS] \"", selector, "\"", Environment.NewLine,
					"  ← ", caller, Environment.NewLine,
					"  url: ", SafeUrl(driver));
				if (snapshot != null)
				{
					text = string.Concat(text, Environment.NewLine, "  snapshot: ", snapshot);
				}
				sink(text);
			}
			catch
			{
			}
		}

		public static string SaveSnapshot(IWebDriver driver)
		{
			if (driver == null)
			{
				return null;
			}
			try
			{
				string dir = Path.Combine(AppPaths.DataDir, "failures", Now().ToString("yyyyMMdd-HHmmss-fff"));
				Directory.CreateDirectory(dir);
				try
				{
					File.WriteAllText(Path.Combine(dir, "page.html"), driver.PageSource);
				}
				catch
				{
				}
				try
				{
					File.WriteAllBytes(Path.Combine(dir, "screen.png"), ((ITakesScreenshot)driver).GetScreenshot().AsByteArray);
				}
				catch
				{
				}
				return dir;
			}
			catch
			{
				return null;
			}
		}

		internal static void ResetForTests()
		{
			Sink = null;
			SnapshotEnabled = false;
			Now = () => DateTime.Now;
			lock (sync)
			{
				lastLogged.Clear();
			}
		}

		private static string FindCaller(StackTrace stackTrace)
		{
			foreach (StackFrame frame in stackTrace.GetFrames())
			{
				MethodBase method = frame.GetMethod();
				Type type = method == null ? null : method.DeclaringType;
				if (type == null || IsInternal(type))
				{
					continue;
				}
				string file = frame.GetFileName();
				string location = file == null
					? ""
					: string.Concat(" (", Path.GetFileName(file), ":", frame.GetFileLineNumber().ToString(), ")");
				return string.Concat(OuterType(type).Name, ".", method.Name, location);
			}
			return "(unknown)";
		}

		private static bool IsInternal(Type type)
		{
			Type outer = OuterType(type);
			return outer == typeof(Util) || outer == typeof(SelectorTrace);
		}

		// 람다·반복기가 만든 중첩 타입(Util+<>c 등)을 바깥 타입으로 정리한다.
		private static Type OuterType(Type type)
		{
			while (type.DeclaringType != null)
			{
				type = type.DeclaringType;
			}
			return type;
		}

		private static string SafeUrl(IWebDriver driver)
		{
			if (driver == null)
			{
				return "(no driver)";
			}
			try
			{
				return driver.Url;
			}
			catch
			{
				return "(url unavailable)";
			}
		}
	}
}
