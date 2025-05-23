﻿using HKX2E;
using Pandora.Models.Patch.Skyrim64.Hkx.Changes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using XmlCake.Linq;

namespace Pandora.Models.Patch.Skyrim64.Hkx.Packfile;

public class PackFileValidator
{
	private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

	private static Regex EventFormat = new(@"[$]{1}eventID{1}[\[]{1}(.+)[\]]{1}[$]{1}");
	private static Regex VarFormat = new(@"[$]{1}variableID{1}[\[]{1}(.+)[\]]{1}[$]{1}");

	private Dictionary<string, int> eventIndices = [];
	private Dictionary<string, int> variableIndices = new(StringComparer.OrdinalIgnoreCase);

	private List<XElement> registeredElements = [];

	public void TrackElement(XElement element) => registeredElements.Add(element);

	private int GetIndexFromMatch(Dictionary<string, int> map, Match match)
	{
		if (!match.Success) return -1;

		if (!map.TryGetValue(match.Groups[1].Value, out int index))
		{
			Logger.Warn($"Validator > Nemesis Event ID > {match.Groups[1].Value} > Index > NOT FOUND");
			return -1;
		}

		return index;
	}
	public bool ValidateEventsAndVariables(PackFileGraph graph)
	{
		var initialEventNames = graph.StringData.eventNames.ToArray();
		var initialEventInfos = graph.Data.eventInfos.ToArray();

		var initialVariableNames = graph.StringData.variableNames.ToArray();
		var initialVariableValues = graph.VariableValueSet.wordVariableValues.ToArray();
		var initialVariableInfos = graph.Data.variableInfos.ToArray();

		int eventLowerBound = (int)graph.InitialEventCount;
		int variableLowerBound = (int)graph.InitialVariableCount;

		List<string> eventNames = [];
		List<hkbEventInfo> eventInfos = [];

		List<string> variableNames = [];
		List<hkbVariableValue> variableValues = [];
		List<hkbVariableInfo> variableInfos = [];

		eventIndices.Clear();
		variableIndices.Clear();

		int duplicateVariableCount = 0;
		int duplicateEventCount = 0;
		for (int i = 0; i < initialEventNames.Length; i++)
		{
			var eventName = initialEventNames[i];
			if (!eventIndices.TryAdd(eventName, i - duplicateEventCount) && i >= eventLowerBound)
			{
				Logger.Warn($"Validator > {graph.ParentProject?.Identifier}~{graph.Name} > Duplicate Event > {eventName} > Index > {i} > SKIPPED");
				duplicateEventCount++;
				continue;
			}
			eventNames.Add(eventName);
			eventInfos.Add(initialEventInfos[i]);
		}
		for (int i = 0; i < initialVariableNames.Length; i++)
		{
			var variableName = initialVariableNames[i];
			if (!variableIndices.TryAdd(variableName, i - duplicateVariableCount) && i >= variableLowerBound)
			{
				Logger.Warn($"Validator > {graph.ParentProject?.Identifier}~{graph.Name} > Duplicate Variable > {variableName} > Index > {i} > SKIPPED");
				duplicateVariableCount++;
				continue;
			}
			variableNames.Add(variableName);
			variableValues.Add(initialVariableValues[i]);
			variableInfos.Add(initialVariableInfos[i]);
		}
		graph.StringData.eventNames = eventNames;
		graph.Data.eventInfos = eventInfos;
		graph.StringData.variableNames = variableNames;
		graph.Data.variableInfos = variableInfos;
		graph.VariableValueSet.wordVariableValues = variableValues;
		return true;
	}
	private void ValidateElementText(XElement element, Dictionary<string, int> eventIndices, Dictionary<string, int> variableIndices)
	{
		string rawValue = element.Value;

		var eventMatch = EventFormat.Matches(rawValue);
		foreach (Match match in eventMatch)
		{
			var index = GetIndexFromMatch(eventIndices, match);

			rawValue = rawValue.Replace(match.Value, index.ToString());
		}

		var varMatch = VarFormat.Matches(element.Value);
		foreach (Match match in varMatch)
		{
			var index = GetIndexFromMatch(variableIndices, match);
			rawValue = rawValue.Replace(match.Value, index.ToString());
		}
		element.SetValue(rawValue);
	}

	private void ValidateElementContent(XElement element, Dictionary<string, int> eventIndices, Dictionary<string, int> variableIndices)
	{

		if (!element.HasElements)
		{
			ValidateElementText(element, eventIndices, variableIndices);
			return;
		}


		foreach (var xelement in element.Elements())
		{

			ValidateElementContent(xelement, eventIndices, variableIndices);
		}

	}
	public void ValidateTrackedElements()
	{
		foreach (XElement element in registeredElements)
		{
			ValidateElementContent(element, eventIndices, variableIndices);
		}
	}
	public void TryValidateClipGenerator(string path, PackFile packFile)
	{
		if (!packFile.Map.TryLookup($"{path}/animationName", out XElement element)) return;
		string clipName = packFile.Map.Lookup($"{path}/name").Value!;
		packFile.ParentProject?.AnimData?.AddDummyClipData(clipName);
	}

	public void Validate(PackFile packFile, params List<IPackFileChange>[] changeLists)
	{
		//if (!ValidateEventsAndVariables(packFile)) return; 

		foreach (var changeList in changeLists)
		{
			foreach (IPackFileChange change in changeList)
			{
				if (!packFile.TryGetXMap(change.Target, out XMapElement element))
				{
					continue;
				}
				ValidateElementContent(element, eventIndices, variableIndices);

			}
		}
	}

}
#pragma warning restore CA1416

