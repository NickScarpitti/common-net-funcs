﻿using static System.Math;

namespace CommonNetFuncs.Core;

public static class DimensionScale
{
    /// <summary>
    /// Get the dimensions to scale a 2d object in a way that maximally fits inside of the maximum constraint dimensions while maintaining its aspect ratio.
    /// </summary>
    /// <param name="originalWidth">Original width dimension to scale</param>
    /// <param name="originalHeight">Original height dimension to scale</param>
    /// <param name="maxWidth">Maximum width constraint for scaled dimensions</param>
    /// <param name="maxHeight">Maximum height constraint for scaled dimensions</param>
    /// <param name="scaleUpToFit">If true, will make dimensions as large as possible to fit in container</param>
    /// <returns>Scaled height and width dimensions that fit into the constraints provided</returns>
    public static (int newWidth, int newHeight) ScaleDimensionsToConstraint(int originalWidth, int originalHeight, int maxWidth, int maxHeight, bool scaleUpToFit = true)
    {
        // If the image dimensions are exactly the same as the container, or if both dimensions are smaller, return the original dimensions.
        if ((originalWidth == maxWidth && originalHeight == maxHeight) || (!scaleUpToFit && originalWidth <= maxWidth && originalHeight <= maxHeight))
        {
            return (originalWidth, originalHeight);
        }

        // Calculate the ratio of the original dimensions to the max dimensions
        decimal[] ratios = [(decimal)maxWidth / originalWidth, (decimal)maxHeight / originalHeight];

        // Use the smaller ratio to ensure the image fits within the container without losing its aspect ratio
        decimal ratio = ratios.Min(); // Use the smallest ratio to ensure the object fits within the container without losing its aspect ratio

        // Calculate the new dimensions and return them
        return ((int)Round(originalWidth * ratio, 0, MidpointRounding.ToZero), (int)Round(originalHeight * ratio, 0, MidpointRounding.ToZero));
    }

    /// <summary>
    /// Get the dimensions to scale a 2d object in a way that maximally fits inside of the maximum constraint dimensions while maintaining its aspect ratio.
    /// </summary>
    /// <param name="originalWidth">Original width dimension to scale</param>
    /// <param name="originalHeight">Original height dimension to scale</param>
    /// <param name="maxWidth">Maximum width constraint for scaled dimensions</param>
    /// <param name="maxHeight">Maximum height constraint for scaled dimensions</param>
    /// <param name="scaleUpToFit">If true, will make dimensions as large as possible to fit in container</param>
    /// <returns>Scaled height and width dimensions that fit into the constraints provided</returns>
    public static (decimal newWidth, decimal newHeight) ScaleDimensionsToConstraint(decimal originalWidth, decimal originalHeight, decimal maxWidth, decimal maxHeight,
        bool scaleUpToFit = true, int? resultDecimalPlaces = null)
    {
        // If the image dimensions are exactly the same as the container, or if both dimensions are smaller, return the original dimensions.
        if ((originalWidth == maxWidth && originalHeight == maxHeight) || (!scaleUpToFit && originalWidth <= maxWidth && originalHeight <= maxHeight))
        {
            return (originalWidth, originalHeight);
        }

        // Calculate the ratio of the original dimensions to the max dimensions
        decimal[] ratios = [maxWidth / originalWidth, maxHeight / originalHeight];

        // Use the smaller ratio to ensure the image fits within the container without losing its aspect ratio
        decimal ratio = ratios.Min();

        // Calculate the new dimensions and return them
        return resultDecimalPlaces == null ? (originalWidth * ratio, originalHeight * ratio) :
           (Round(originalWidth * ratio, (int)resultDecimalPlaces, MidpointRounding.ToZero), Round(originalHeight * ratio, (int)resultDecimalPlaces, MidpointRounding.ToZero));
    }

    //Credit GPT-4
    /// <summary>
    /// Get the dimensions to scale a 3d object in a way that maximally fits inside of the maximum constraint dimensions while maintaining its aspect ratio.
    /// </summary>
    /// <param name="originalWidth">Original width dimension to scale</param>
    /// <param name="originalHeight">Original height dimension to scale</param>
    /// <param name="originalDepth">Original depth dimension to scale</param>
    /// <param name="maxWidth">Maximum width constraint for scaled dimensions</param>
    /// <param name="maxHeight">Maximum height constraint for scaled dimensions</param>
    /// <param name="maxDepth">Maximum depth constraint for scaled dimensions</param>
    /// <param name="scaleUpToFit">If true, will make dimensions as large as possible to fit in container</param>
    /// <returns>Scaled height and width dimensions that fit into the constraints provided</returns>
    public static (int newWidth, int newHeight, int newDepth) ScaleDimensionsToConstraint(int originalWidth, int originalHeight, int originalDepth, int maxWidth, int maxHeight,
        int maxDepth, bool scaleUpToFit = true)
    {
        // If the object dimensions are exactly the same as the container, or if all dimensions are smaller, return the original dimensions.
        if (originalWidth == maxWidth && originalHeight == maxHeight && originalDepth == maxDepth || !scaleUpToFit && originalWidth <= maxWidth && originalHeight <= maxHeight && originalDepth <= maxDepth)
        {
            return (originalWidth, originalHeight, originalDepth);
        }

        // Calculate the ratio of the original dimensions to the max dimensions
        decimal[] ratios = [(decimal)maxHeight / originalHeight, (decimal)maxHeight / originalHeight, (decimal)maxDepth / originalDepth];

        // Use the smallest ratio to ensure the object fits within the container without losing its aspect ratio
        decimal ratio = ratios.Min();

        // Calculate the new dimensions and return them
        return ((int)Round(originalWidth * ratio, 0, MidpointRounding.ToZero), (int)Round(originalHeight * ratio, 0, MidpointRounding.ToZero), (int)Round(originalDepth * ratio, 0, MidpointRounding.ToZero));
    }

    /// <summary>
    /// Get the dimensions to scale a 3d object in a way that maximally fits inside of the maximum constraint dimensions while maintaining its aspect ratio.
    /// </summary>
    /// <param name="originalWidth">Original width dimension to scale</param>
    /// <param name="originalHeight">Original height dimension to scale</param>
    /// <param name="originalDepth">Original depth dimension to scale</param>
    /// <param name="maxWidth">Maximum width constraint for scaled dimensions</param>
    /// <param name="maxHeight">Maximum height constraint for scaled dimensions</param>
    /// <param name="maxDepth">Maximum depth constraint for scaled dimensions</param>
    /// <param name="scaleUpToFit">If true, will make dimensions as large as possible to fit in container</param>
    /// <returns>Scaled height and width dimensions that fit into the constraints provided</returns>
    public static (decimal newWidth, decimal newHeight, decimal newDepth) ScaleDimensionsToConstraint(decimal originalWidth, decimal originalHeight, decimal originalDepth,
        decimal maxWidth, decimal maxHeight, decimal maxDepth, bool scaleUpToFit = true, int? resultDecimalPlaces = null)
    {
        // If the object dimensions are exactly the same as the container, or if all dimensions are smaller, return the original dimensions.
        if (originalWidth == maxWidth && originalHeight == maxHeight && originalDepth == maxDepth || !scaleUpToFit && originalWidth <= maxWidth && originalHeight <= maxHeight && originalDepth <= maxDepth)
        {
            return (originalWidth, originalHeight, originalDepth);
        }

        // Calculate the ratio of the original dimensions to the max dimensions
        decimal[] ratios = [maxWidth / originalWidth, maxHeight / originalHeight, maxDepth / originalDepth];

        // Use the smallest ratio to ensure the object fits within the container without losing its aspect ratio
        decimal ratio = ratios.Min();

        // Calculate the new dimensions and return them
        return resultDecimalPlaces == null ? (originalWidth * ratio, originalHeight * ratio, originalDepth * ratio) :
            (Round(originalWidth * ratio, (int)resultDecimalPlaces, MidpointRounding.ToZero), Round(originalHeight * ratio, (int)resultDecimalPlaces, MidpointRounding.ToZero), Round(originalDepth * ratio, (int)resultDecimalPlaces, MidpointRounding.ToZero));
    }
}
