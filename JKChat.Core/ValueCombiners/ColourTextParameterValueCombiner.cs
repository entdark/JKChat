using System;
using System.Collections.Generic;
using System.Linq;

using MvvmCross.Binding.Bindings.SourceSteps;

using MvvmCross.Binding.Combiners;

namespace JKChat.Core.ValueCombiners {
	public class ColourTextParameterValueCombiner : MvxValueCombiner {
		public override Type SourceType(IEnumerable<IMvxSourceStep> steps) {
			return typeof(ColourTextParameter);
		}
		public override bool TryGetValue(IEnumerable<IMvxSourceStep> steps, out object value) {
			var stepsList = steps as IList<IMvxSourceStep> ?? steps.ToList();
			if (stepsList.Count == 2) {
				if (stepsList[0].GetValue() is bool parseUri && stepsList[1].GetValue() is bool parseShadow) {
					value = new ColourTextParameter() {
						ParseUri = parseUri,
						ParseShadow = parseShadow
					};
					return true;
				}
			}
			value = null;
			return false;
		}
	}

	public class ColourTextParameter {
		public bool ParseUri { get; init; }
		public bool ParseShadow { get; init; }
	}
}