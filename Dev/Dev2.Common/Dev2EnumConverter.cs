/*
*  Warewolf - Once bitten, there's no going back
*  Copyright 2018 by Warewolf Ltd <alpha@warewolf.io>
*  Licensed under GNU Affero General Public License 3.0 or later.
*  Some rights reserved.
*  Visit our website for more information <http://warewolf.io/>
*  AUTHORS <http://warewolf.io/authors.php> , CONTRIBUTORS <http://warewolf.io/contributors.php>
*  @license GNU Affero General Public License <http://www.gnu.org/licenses/agpl-3.0.html>
*/

using Dev2.Common.ExtMethods;
using System;
using System.Collections.Generic;
using Warewolf.Resource.Errors;

namespace Dev2.Common.Interfaces.Enums.Enums
{
    public static class Dev2EnumConverter
    {
        public static IList<string> ConvertEnumsTypeToStringList<tEnum>() where tEnum : struct
        {
            var enumType = typeof(tEnum);

            IList<string> result = new List<string>();

            
            foreach (object value in Enum.GetValues(enumType))
            
            {
                result.Add((value as Enum).GetDescription());
            }

            return result;
        }

        public static string ConvertEnumValueToString(Enum value)
        {
            var type = value.GetType();
            if (!type.IsEnum)
            {
                throw new InvalidOperationException(ErrorResource.ExpectedEnumerationTypeParameter);
            }

            return value.GetDescription();
        }

        public static object GetEnumFromStringDiscription(string discription, Type type)
        {
            if (!type.IsEnum)
            {
                throw new InvalidOperationException(ErrorResource.ExpectedEnumerationTypeParameter);
            }

            foreach (object value in Enum.GetValues(type))
            
            {
                if ((value as Enum).GetDescription() == discription)
                {
                    return value;
                }
            }
            return null;
        }
    }
}