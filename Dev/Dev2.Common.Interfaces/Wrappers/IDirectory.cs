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
using System.IO;

namespace Dev2.Common.Interfaces.Wrappers
{
    public interface IDirectory
    {
        DirectoryInfo GetParent(string path);
        string[] GetFiles(string path);
        string CreateIfNotExists(string path);
        string[] GetLogicalDrives();
        bool Exists(string path);
        string[] GetFileSystemEntries(string path);
        string[] GetFileSystemEntries(string path, string searchPattern);
        string[] GetDirectories(string workspacePath);
        string[] GetDirectories(string path, string pattern);
        void Move(string directoryStructureFromPath, string directoryStructureToPath);
        void Delete(string directoryStructureFromPath, bool recursive);
        DirectoryInfo CreateDirectory(string dir);
        IEnumerable<string> EnumerateFiles(string path);
        IEnumerable<string> EnumerateFileSystemEntries(string path);
        IEnumerable<string> EnumerateDirectories(string path);
        IEnumerable<string> EnumerateDirectories(string path, string pattern);
        IEnumerable<string> EnumerateFiles(string path, string pattern);
        IEnumerable<string> EnumerateFileSystemEntries(string path, string pattern);        
    }
}