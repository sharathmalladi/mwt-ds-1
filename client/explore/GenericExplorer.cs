﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Research.MultiWorldTesting.ExploreLibrary
{
    public class GenericExplorerState
    {
        [JsonIgnore]
        public float Probability { get; set; }
    }

    /// <summary>
	/// The generic exploration class.
	/// </summary>
	/// <remarks>
	/// GenericExplorer provides complete flexibility.  You can create any
	/// distribution over actions desired, and it will draw from that.
	/// </remarks>
	/// <typeparam name="TContext">The Context type.</typeparam>
    public class GenericExplorer : BaseExplorer<int, float[]>
	{
		/// <summary>
		/// The constructor is the only public member, because this should be used with the MwtExplorer.
		/// </summary>
		/// <param name="defaultScorer">A function which outputs the probability of each action.</param>
		/// <param name="numActions">The number of actions to randomize over.</param>
        public GenericExplorer()
		{
        }

        public override ExplorerDecision<int> MapContext(PRG random, float[] weights, int numActions)
        {
            int numWeights = weights.Length;
            if (numActions != int.MaxValue && numWeights != numActions)
                throw new ArgumentException("The number of weights returned by the scorer must equal number of actions");

            // Create a discrete_distribution based on the returned weights. This class handles the
            // case where the sum of the weights is < or > 1, by normalizing agains the sum.
            float total = 0f;
            for (int i = 0; i < numWeights; i++)
            {
                if (weights[i] < 0)
                    throw new ArgumentException("Scores must be non-negative.");

                total += weights[i];
            }

            if (total == 0)
                throw new ArgumentException("At least one score must be positive.");

            float draw = random.UniformUnitInterval();

            float sum = 0f;
            float actionProbability = 0f;
            int actionIndex = numWeights - 1;
            for (int i = 0; i < numWeights; i++)
            {
                weights[i] = weights[i] / total;
                sum += weights[i];
                // This needs to be >=, not >, in case the random draw = 1.0, since sum would never
                // be > 1.0 and the loop would exit without assigning the right action probability.
                if (sum >= draw)
                {
                    actionIndex = i;
                    actionProbability = weights[i];
                    break;
                }
            }

            actionIndex++;

            // action id is one-based
            return ExplorerDecision.Create(
                actionIndex,
                new GenericExplorerState { Probability = actionProbability },
                true);
        }
    }

    /// <summary>
    /// The generic exploration class.
    /// </summary>
    /// <remarks>
    /// GenericExplorer provides complete flexibility.  You can create any
    /// distribution over actions desired, and it will draw from that.
    /// </remarks>
    /// <typeparam name="TContext">The Context type.</typeparam>
    public sealed class GenericExplorerSampleWithoutReplacement 
        : BaseExplorer<int[], float[]>
    {
        private readonly GenericExplorer explorer;

        /// <summary>
        /// The constructor is the only public member, because this should be used with the MwtExplorer.
        /// </summary>
        /// <param name="defaultScorer">A function which outputs the probability of each action.</param>
        /// <param name="numActions">The number of actions to randomize over.</param>
        public GenericExplorerSampleWithoutReplacement()
        {
            this.explorer = new GenericExplorer();
        }

        public override void EnableExplore(bool explore)
        {
            base.EnableExplore(explore);
            this.explorer.EnableExplore(explore);
        }

        public override ExplorerDecision<int[]> MapContext(PRG random, float[] weights, int numActions)
        {
            var decision = this.explorer.MapContext(random, weights, numActions);

            float actionProbability = 0f;
            int[] chosenActions = MultiActionHelper.SampleWithoutReplacement(weights, weights.Length, random, ref actionProbability);

            // action id is one-based
            return ExplorerDecision.Create(chosenActions,
                new GenericExplorerState { Probability = actionProbability },
                true);
        }
    }
}
