/*
*  Warewolf - Once bitten, there's no going back
*  Copyright 2018 by Warewolf Ltd <alpha@warewolf.io>
*  Licensed under GNU Affero General Public License 3.0 or later. 
*  Some rights reserved.
*  Visit our website for more information <http://warewolf.io/>
*  AUTHORS <http://warewolf.io/authors.php> , CONTRIBUTORS <http://warewolf.io/contributors.php>
*  @license GNU Affero General Public License <http://www.gnu.org/licenses/agpl-3.0.html>
*/

using System.Collections.Generic;

namespace Dev2.Studio.Interfaces
{
    /// <summary>
    /// Defines the requirements for a deploy DTO.
    /// </summary>
    public interface IDeployDto
    {
        /// <summary>
        /// Gets or sets the resource models.
        /// </summary>
        IList<IResourceModel> ResourceModels
        {
            get;
            set;
        }
        bool DeployTests { get; set; }

    }
}
