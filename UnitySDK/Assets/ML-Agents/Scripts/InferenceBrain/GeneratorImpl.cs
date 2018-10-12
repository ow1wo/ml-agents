﻿using UnityEngine.MachineLearning.InferenceEngine;
using System.Collections.Generic;
using System;
using UnityEngine.MachineLearning.InferenceEngine.Util;
using System.Linq;

namespace MLAgents.InferenceBrain
{
    /// <summary>
    /// Reshapes a Tensor so that its first dimension becomes equal to the current batch size
    /// and initializes its content to be zeros. Will only work on 2-dimensional tensors.
    /// The second dimension of the Tensor will not be modified.
    /// </summary>
    public class BiDimensionalOutputGenerator : TensorGenerator.Generator
    {
        public void Generate(Tensor tensor, int batchSize, Dictionary<Agent, AgentInfo> agentInfo)
        {
            var shapeSecondAxis = tensor.Shape[1];
            tensor.Shape[0] = batchSize;
            if (tensor.ValueType == Tensor.TensorType.FloatingPoint)
            {
                tensor.Data = new float[batchSize, shapeSecondAxis];
            }
            else
            {
                tensor.Data = new int[batchSize, shapeSecondAxis];
            }
        }
    }

    /// <summary>
    /// Generates the Tensor corresponding to the BatchSize input : Will be a one dimensional
    /// integer array of size 1 containing the batch size.
    /// </summary>
    public class BatchSizeGenerator : TensorGenerator.Generator
    {
        public void Generate(Tensor tensor, int batchSize, Dictionary<Agent, AgentInfo> agentInfo)
        {
            tensor.Data = new int[] {batchSize};
        }
    }

    /// <summary>
    /// Generates the Tensor corresponding to the SequenceLength input : Will be a one
    /// dimensional integer array of size 1 containing 1.
    /// Note : the sequence length is always one since recurrent networks only predict for
    /// one step at the time.
    /// </summary>
    public class SequenceLengthGenerator : TensorGenerator.Generator
    {
        public void Generate(Tensor tensor, int batchSize, Dictionary<Agent, AgentInfo> agentInfo)
        {
            tensor.Data = new int[] {1};
        }
    }

    /// <summary>
    /// Generates the Tensor corresponding to the VectorObservation input : Will be a two
    /// dimensional float array of dimension [batchSize x vectorObservationSize].
    /// It will use the Vector Observation data contained in the agentInfo to fill the data
    /// of the tensor.
    /// </summary>
    public class VectorObservationGenerator : TensorGenerator.Generator
    {
        public void Generate(Tensor tensor, int batchSize, Dictionary<Agent, AgentInfo> agentInfo)
        {
            tensor.Shape[0] = batchSize;
            var vecObsSizeT = tensor.Shape[1];
            tensor.Data = new float[batchSize, vecObsSizeT];
            var agentIndex = 0;
            foreach (var agent in agentInfo.Keys)
            {
                var vectorObs = agentInfo[agent].stackedVectorObservation;
                for (var j = 0; j < vecObsSizeT; j++)
                {
                    tensor.Data.SetValue(vectorObs[j], new int[2] {agentIndex, j});
                }
                agentIndex++;
            }
        }
    }

    /// <summary>
    /// Generates the Tensor corresponding to the Recurrent input : Will be a two
    /// dimensional float array of dimension [batchSize x memorySize].
    /// It will use the Memory data contained in the agentInfo to fill the data
    /// of the tensor.
    /// </summary>
    public class RecurrentInputGenerator : TensorGenerator.Generator
    {
        public void Generate(Tensor tensor, int batchSize, Dictionary<Agent, AgentInfo> agentInfo)
        {
            tensor.Shape[0] = batchSize;
            var memorySize = tensor.Shape[1];
            tensor.Data = new float[batchSize, memorySize];
            var agentIndex = 0;
            foreach (var agent in agentInfo.Keys)
            {
                var memory = agentInfo[agent].memories;
                if (memory == null)
                {
                    agentIndex++;
                    continue;
                }
                for (var j = 0; j < Math.Min(memorySize, memory.Count); j++)
                {
                    if (j >= memory.Count)
                    {
                        break;
                    }
                    tensor.Data.SetValue(memory[j], new int[2] {agentIndex, j});
                }
                agentIndex++;
            }
        }
    }

    /// <summary>
    /// Generates the Tensor corresponding to the Previous Action input : Will be a two
    /// dimensional integer array of dimension [batchSize x actionSize].
    /// It will use the previous action data contained in the agentInfo to fill the data
    /// of the tensor.
    /// </summary>
    public class PreviousActionInputGenerator : TensorGenerator.Generator
    {
        public void Generate(Tensor tensor, int batchSize, Dictionary<Agent, AgentInfo> agentInfo)
        {
            if (tensor.ValueType != Tensor.TensorType.Integer)
            {
                throw new NotImplementedException(
                    "Previous Action Inputs are only valid for discrete control");
            }

            tensor.Shape[0] = batchSize;
            var actionSize = tensor.Shape[1];
            tensor.Data = new int[batchSize, actionSize];
            var agentIndex = 0;
            foreach (var agent in agentInfo.Keys)
            {
                var pastAction = agentInfo[agent].storedVectorActions;
                for (var j = 0; j < actionSize; j++)
                {
                    tensor.Data.SetValue((int) pastAction[j], new int[2] {agentIndex, j});
                }

                agentIndex++;
            }
        }
    }

    /// <summary>
    /// Generates the Tensor corresponding to the Action Mask input : Will be a two
    /// dimensional float array of dimension [batchSize x numActionLogits].
    /// It will use the Action Mask data contained in the agentInfo to fill the data
    /// of the tensor.
    /// </summary>
    public class ActionMaskInputGenerator : TensorGenerator.Generator
    {
        public void Generate(Tensor tensor, int batchSize, Dictionary<Agent, AgentInfo> agentInfo)
        {
            tensor.Shape[0] = batchSize;
            var maskSize = tensor.Shape[1];
            tensor.Data = new float[batchSize, maskSize];
            var agentIndex = 0;
            foreach (var agent in agentInfo.Keys)
            {
                var maskList = agentInfo[agent].actionMasks;
                for (var j = 0; j < maskSize; j++)
                {
                    var isUnmasked = (maskList != null && maskList[j]) ? 0.0f : 1.0f;
                    tensor.Data.SetValue(isUnmasked, new int[2] {agentIndex, j});
                }
                agentIndex++;
            }
        }
    }

    /// <summary>
    /// Generates the Tensor corresponding to the Epsilon input : Will be a two
    /// dimensional float array of dimension [batchSize x actionSize].
    /// It will use the generate random input data from a normal Distribution.
    /// </summary>
    public class RandomNormalInputGenerator : TensorGenerator.Generator
    {
        private RandomNormal _randomNormal;
        
        public RandomNormalInputGenerator(int seed)
        {
            _randomNormal = new RandomNormal(seed);
        }
        
        public void Generate(Tensor tensor, int batchSize, Dictionary<Agent, AgentInfo> agentInfo)
        {
            tensor.Shape[0] = batchSize;
            var actionSize = tensor.Shape[1];
            tensor.Data = new float[batchSize, actionSize];
            _randomNormal.FillTensor(tensor);
        }
    }

    /// <summary>
    /// Generates the Tensor corresponding to the Visual Observation input : Will be a 4
    /// dimensional float array of dimension [batchSize x width x heigth x numChannels].
    /// It will use the Texture input data contained in the agentInfo to fill the data
    /// of the tensor.
    /// </summary>
    public class VisualObservationInputGenerator : TensorGenerator.Generator
    {
        private int _index;
        private bool _grayScale;
        public VisualObservationInputGenerator(int index, bool grayScale)
        {
            _index = index;
            _grayScale = grayScale;
        }
        
        public void Generate(Tensor tensor, int batchSize, Dictionary<Agent, AgentInfo> agentInfo)
        {
            var textures = agentInfo.Keys.Select(
                agent => agentInfo[agent].visualObservations[_index]).ToList();
            tensor.Data = Utilities.TextureToFloatArray(textures, _grayScale);
        } 
    } 
}
