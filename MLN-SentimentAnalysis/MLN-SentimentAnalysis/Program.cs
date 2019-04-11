﻿using Microsoft.ML;
using Microsoft.ML.Data;
using System;

namespace MLN_SentimentAnalysis
{
    class Program
    {
        static readonly string _path = "..\\..\\..\\Data\\yelp_labelled.tsv";

        static readonly string[] _samples =
        {
            "The food was great and the service was excellent",
            "The food was bland and the service was average",
            "The food was bland and the service was below average",
            "The only thing worse than the food was the terrible service",
            "I wouldn't let my dog eat here"
        };

        static void Main(string[] args)
        {
            var context = new MLContext(seed: 0);

            // Load the data
            var data = context.Data.LoadFromTextFile<SentimentData>(_path, hasHeader: false);

            // Split the data into a training set and a test set
            var trainTestData = context.BinaryClassification.TrainTestSplit(data, testFraction: 0.2, seed: 0);
            var trainData = trainTestData.TrainSet;
            var testData = trainTestData.TestSet;

            // Build and train the model
            var pipeline = context.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: "SentimentText")
                .Append(context.BinaryClassification.Trainers.FastTree(numLeaves: 50, numTrees: 50, minDatapointsInLeaves: 20));

            Console.WriteLine("Training the model...");
            var model = pipeline.Fit(trainData);

            // Evaluate the model
            var predictions = model.Transform(testData);
            var metrics = context.BinaryClassification.Evaluate(predictions, "Label");

            Console.WriteLine();
            Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
            Console.WriteLine($"AUC: {metrics.Auc:P2}");
            Console.WriteLine($"F1: {metrics.F1Score:P2}");

            // Use the model to make predictions
            var predictor = model.CreatePredictionEngine<SentimentData, SentimentPrediction>(context);

            foreach(var sample in _samples)
            {
                var input = new SentimentData { SentimentText = sample };
                var prediction = predictor.Predict(input);

                Console.WriteLine();
                Console.WriteLine($"{input.SentimentText}");
                Console.WriteLine($"Sentiment score: {prediction.Probability}");
                Console.WriteLine($"Sentiment: {(Convert.ToBoolean(prediction.Prediction) ? "Positive" : "Negative")}");
            }

            Console.WriteLine();
        }
    }

    public class SentimentData
    {
        [LoadColumn(0)]
        public string SentimentText;

        [LoadColumn(1), ColumnName("Label")]
        public bool Sentiment;
    }

    public class SentimentPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }
        public float Probability { get; set; }
    }
}