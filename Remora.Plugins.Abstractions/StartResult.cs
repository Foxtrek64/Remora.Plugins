//
//  StartResult.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Remora.Results;

namespace Remora.Plugins.Abstractions;

/// <summary>
/// The results from a plugin's StartAsync() method.
/// </summary>
public readonly struct StartResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartResult"/> struct.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="migration">The migration delegate.</param>
    private StartResult(Result result, Func<CancellationToken, Task<Result>> migration)
    {
        Result = result;
        Migration = migration;
    }

    /// <summary>
    /// Gets the Result value from the return of the plugin's StartAsync() method.
    /// </summary>
    public Result Result { get; }

    /// <summary>
    /// Gets the method used to perform any migrations required by the plugin.
    /// </summary>
    public Func<CancellationToken, Task<Result>> Migration { get; }

    /// <summary>
    /// Converts a Result into a <see cref="StartResult"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The <see cref="StartResult"/>.</returns>
    /// <remarks>This sets migration to a dummy delegate that does nothing but returns a successful result.</remarks>
    public static implicit operator StartResult(Result result)
    {
        return new(
            result,
            async (ct) =>
            {
                await Task.Delay(0, ct);
                return Result.FromSuccess();
            });
    }

    /// <summary>
    /// Converts a Migration delegate into a <see cref="StartResult"/>.
    /// </summary>
    /// <param name="migration">The migration delegate.</param>
    /// <returns>The <see cref="StartResult"/>.</returns>
    public static implicit operator StartResult(Func<CancellationToken, Task<Result>> migration)
    {
        return new(Result.FromSuccess(), migration);
    }
}
