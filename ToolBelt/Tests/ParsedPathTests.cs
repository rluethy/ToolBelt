using System;
using System.IO;
using Toaster;
using ToolBelt;

namespace ToolBelt.Tests
{
    [TestClass] 
    public class ParsedPathTests
    {
        [TestMethod] public void TestOperators()
        {
            ParsedPath pp = new ParsedPath("file.txt", PathType.Automatic);
            
            Assert.IsFalse(pp == null);
            Assert.IsFalse(null == pp);
            Assert.IsTrue(pp != null);
            Assert.IsTrue(null != pp);
            Assert.IsFalse(pp.Equals(null));
            Assert.IsFalse(ParsedPath.Equals(pp, null));
            Assert.IsFalse(ParsedPath.Equals(null, pp));
            
            int hash = pp.GetHashCode();
            
            Assert.IsFalse(pp == ParsedPath.Empty);
        }

        [TestMethod]
        public void TestCompareTo()
        {
            ParsedPath ppA = new ParsedPath(@"c:\blah\a.txt", PathType.File);
            ParsedPath ppB = new ParsedPath(@"c:\blah\a.txt", PathType.File);
            ParsedPath ppB2 = new ParsedPath(@"c:\blah\b.txt", PathType.File);
            ParsedPath ppC = new ParsedPath(@"c:\blah\a.txt", PathType.File);
            ParsedPath ppC2 = new ParsedPath(@"c:\blah\b.txt", PathType.File);
            string p = @"c:\blah\a.txt";

            // MSDN gives the following axioms for CompareTo()
            Assert.AreEqual(0, ppA.CompareTo(ppB));
            Assert.AreEqual(0, ppB.CompareTo(ppA));
            Assert.AreEqual(0, ppB.CompareTo(ppC));
            Assert.AreEqual(0, ppA.CompareTo(ppC));
            int x = ppA.CompareTo(ppB2);
            Assert.IsTrue(x != 0);
            Assert.IsTrue(Math.Sign(x) == -Math.Sign(ppB2.CompareTo(ppA)));
            int y = ppB.CompareTo(ppC2);
            Assert.IsTrue(Math.Sign(x) == Math.Sign(y));
            Assert.AreEqual(Math.Sign(x), ppA.CompareTo(ppC2));

            // Need to be able to compare ParsePath to String
            Assert.AreEqual(0, ppA.CompareTo(p));
        }

        [TestMethod] public void ConstructParsedPath() 
        {
            // Test some good paths
            AssertPathParts(@"\", PathType.Automatic, "", "", @"", @"\", "", "");
            AssertPathParts(@".txt", PathType.Automatic, "", "", "", "", "", ".txt");
            AssertPathParts(@"*.txt", PathType.File, "", "", "", "", @"*", @".txt");
            AssertPathParts(@"..\*.txt", PathType.Automatic, "", "", "", @"..\", @"*", @".txt");
            AssertPathParts(@".", PathType.Automatic, "", "", "", "", "", "");
            AssertPathParts(@".", PathType.Directory, "", "", "", @".\", "", "");
            AssertPathParts(@"..", PathType.Automatic, "", "", "", "", "", "");
            AssertPathParts(@"..", PathType.Directory, "", "", "", @"..\", "", "");
            
            AssertPathParts(@"c:", PathType.Automatic, "", "", @"c:", "", "", "");
            AssertPathParts(@"c:", PathType.Volume, "", "", @"c:", "", "", "");
            AssertPathParts(@"c:\", PathType.Directory, "", "", @"c:", @"\", "", "");
            AssertPathParts(@"c:foo.txt", PathType.Automatic, "", "", @"c:", "", "foo", ".txt");
            AssertPathParts(@"c:.txt", PathType.Automatic, "", "", @"c:", "", "", ".txt");
            AssertPathParts(@"c:....txt", PathType.Automatic, "", "", @"c:", "", "...", ".txt");
            AssertPathParts(@"c:foo.txt", PathType.Directory, "", "", @"c:", @"foo.txt\", "", "");
            AssertPathParts(@"c:blah\blah\", PathType.Automatic, "", "", @"c:", @"blah\blah\", "", "");
            AssertPathParts(@"c:blah\blah", PathType.Directory, "", "", @"c:", @"blah\blah\", "", "");
            AssertPathParts(@"c:blah\blah", PathType.Automatic, "", "", @"c:", @"blah\", "blah", "");
            AssertPathParts(@"c:\test", PathType.Directory, "", "", @"c:", @"\test\", @"", "");
            AssertPathParts(@"c:\test", PathType.File, "", "", @"c:", @"\", @"test", "");
            AssertPathParts(@"c:\test.txt", PathType.File, "", "", @"c:", @"\", @"test", @".txt");
            AssertPathParts(@"c:\whatever\test.txt", PathType.File, "", "", @"c:", @"\whatever\", @"test", @".txt");
            AssertPathParts(@"c:\test\..\temp\??.txt", PathType.File, "", "", @"c:", @"\test\..\temp\", @"??", @".txt");
            AssertPathParts(@"C:/test\the/use of\forward/slashes\a.txt", PathType.Automatic, 
                "", "", @"C:", @"\test\the\use of\forward\slashes\", "a", @".txt");
            AssertPathParts(@"   C:\ remove \  trailing \     spaces   \  file.txt   ", PathType.Automatic, 
                "", "", @"C:", @"\ remove\  trailing\     spaces\", "  file", @".txt");
            AssertPathParts(@"C:\remove.\trailing....\dots . .\and\spaces. . \file.txt. . .", PathType.Automatic, 
                "", "", @"C:", @"\remove\trailing\dots\and\spaces\", "file", @".txt");
            AssertPathParts(@"  .  . a . really . strange . name .. . . \a\b\c\...", PathType.Directory, 
                "", "", "", @".  . a . really . strange . name\a\b\c\...\", "", "");
            AssertPathParts(@"  ""  C:\this path is quoted\file.txt  ""  ", PathType.Automatic, 
                "", "", @"C:", @"\this path is quoted\", "file", @".txt");
            AssertPathParts(@"c:\assembly\Company.Product.Subcomponent.dll", PathType.Automatic, 
                "", "", @"c:", @"\assembly\", "Company.Product.Subcomponent", @".dll");
            
            AssertPathParts(@"\\machine\share", PathType.Automatic, @"\\machine", @"\share", "", "", "", "");
            AssertPathParts(@"\\machine\share\", PathType.Automatic, @"\\machine", @"\share", "", @"\", @"", @"");
            AssertPathParts(@"\\computer\share\test\subtest\abc.txt", PathType.Automatic, 
                @"\\computer", @"\share", "", @"\test\subtest\", @"abc", @".txt");

            // Test special last folder property
            Assert.AreEqual("c", new ParsedPath(@"c:\a\b\c\", PathType.Automatic).LastDirectoryNoSeparator);
            Assert.AreEqual(@"\", new ParsedPath(@"c:\", PathType.Automatic).LastDirectoryNoSeparator);

            // Test some bad paths
            AssertBadPath(@"", PathType.File);
            AssertBadPath(@":", PathType.File);
            AssertBadPath(@"\", PathType.File);
            AssertBadPath(@"\  \", PathType.Directory);
            AssertBadPath(@"  ""    ""  ", PathType.Automatic);
            AssertBadPath(@"c:\*&#()_@\~{}|<>", PathType.Automatic);

            AssertBadPath(@"c:", PathType.Directory);
            AssertBadPath(@"c: \file.txt", PathType.File);
            AssertBadPath(@"c:\a\b\*\c\", PathType.Automatic);
            AssertBadPath(@"c:\dir\file.txt", PathType.Volume);
            AssertBadPath(@"c:\dir\\file.txt", PathType.File);

            AssertBadPath(@"\\", PathType.Volume);
            AssertBadPath(@"\\computer", PathType.Volume);
            AssertBadPath(@"\\computer\", PathType.Volume);
            AssertBadPath(@"\\computer\\", PathType.Volume);
            AssertBadPath(@"\\machine\share\\bad", PathType.Directory);
        }

        [TestMethod] public void MakeFullPath()
        {
            // Test some good paths
            AssertPathPartsFull(@".txt", 
                @"c:\temp\", "", "", @"c:", @"\temp\", "", ".txt");
            AssertPathPartsFull(@"c:\test\..\temp\??.txt", 
                null, "", "", @"c:", @"\temp\", @"??", @".txt");
            AssertPathPartsFull(@"\test\..\temp\abc.txt", 
                @"c:\a\b\", "", "", "c:", @"\temp\", @"abc", @".txt");
            AssertPathPartsFull(@"test\..\temp\abc.txt", 
                @"c:\a\b\", "", "", @"c:", @"\a\b\temp\", @"abc", @".txt");
            AssertPathPartsFull(@".\test\....\temp\abc.txt", 
                @"c:\a\b\c\", "", "", "c:", @"\a\temp\", @"abc", @".txt");
            AssertPathPartsFull(@"...\test\abc.txt", 
                @"c:\a\b\c\", "", "", "c:", @"\a\test\", @"abc", @".txt");
            AssertPathPartsFull(@"C:\temp\..yes...this...is.a..legal.file.name....\and...\so...\...is\.this.\blah.txt", 
                null, "", "", @"C:", 
                @"\temp\..yes...this...is.a..legal.file.name\and\so\...is\.this\", 
                @"blah", @".txt");

            // Test that using the current directory works
            ParsedPath pp = new ParsedPath(Environment.CurrentDirectory, PathType.Directory);
            AssertPathPartsFull(@"test.txt", null, pp.Machine, pp.Share, pp.Drive, pp.Directory, "test", ".txt");

            // Test some bad paths
            AssertBadPathFull(@"c:\test\..\..\temp\abc.txt", null);  // Too many '..'s
            AssertBadPathFull(@"test\......\temp\.\abc.txt", @"c:\");  // Too many '....'s
        }
        
        [TestMethod] public void MakeRelativePath()
        {
            AssertPathPartsRelative(@"c:\a\p.q", @"c:\a\", @".\");
            AssertPathPartsRelative(@"c:\a\", @"c:\a\b\c\p.q", @"..\..\"); 
            AssertPathPartsRelative(@"c:\a\b\c\p.q", @"c:\a\", @".\b\c\"); 
            AssertPathPartsRelative(@"c:\a\b\c\p.q", @"c:\a\x\y\", @"..\..\b\c\");

            AssertBadPathPartsRelative(@"..\a.txt", @"c:\a\b");
            AssertBadPathPartsRelative(@"a.txt", @"b");
        }

        [TestMethod] public void MakeParentPath()
        {
            // Test going up one parent
            AssertParentPath(@"c:\a\b\c\p.q", -1, null, "", "", "c:", @"\a\b\", "p", ".q"); 
            AssertParentPath(@"c:\a\b\c\", -1, null, "", "", "c:", @"\a\b\", "", ""); 
            AssertParentPath(@"c:\a\b\..\c\", -1, null, "", "", "c:", @"\a\", "", ""); 
            AssertParentPath(@"..\c\", -1, @"c:\a\b\", "", "", "c:", @"\a\", "", ""); 
            AssertParentPath(@"\\machine\share\a\b\c\..\d\foo.bar", -1, null, 
                @"\\machine", @"\share", "", @"\a\b\", "foo", ".bar"); 
            
            // Test going up multiple parents
            AssertParentPath(@"c:\a\b\c\d\e\", -3, null, "", "", "c:", @"\a\b\", "", "");
            AssertParentPath(@"c:\a\b\c\d\e\", -5, null, "", "", "c:", @"\", "", "");

            // Test bad stuff
            AssertBadParentPath(@"c:\", -1); // Already root
            AssertBadParentPath(@"c:\a\", 2); // Positive index not allowed
            AssertBadParentPath(@"c:\a\b\c\", -4); // Too many parent levels
        }

        #region Assert paths parts
        private void AssertPathParts(
            string path,
            PathType type, 
            string machine,
            string share,
            string drive,
            string directory,
            string file,
            string extension)
        {
            ParsedPath pp = new ParsedPath(path, type);
        
            Assert.AreEqual(machine, pp.Machine);
            Assert.AreEqual(share, pp.Share);
            Assert.AreEqual(drive, pp.Drive);
            Assert.AreEqual(directory, pp.Directory);
            Assert.AreEqual(file, pp.File);
            Assert.AreEqual(extension, pp.Extension);
        }
        
        private void AssertBadPath(
            string path, 
            PathType type)
        {
            try
            {
                ParsedPath pp = new ParsedPath(path, type);
                Assert.Fail("Badly formed path not caught");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }
        }
        #endregion		

        #region Assert path parts with MakeFullPath()
        private void AssertPathPartsFull(
            string path,
            string baseDir,
            string machine,
            string share,
            string drive,
            string directory,
            string file,
            string extension)
        {
            ParsedPath pp;
            
            if (baseDir != null)
                pp = new ParsedPath(path, PathType.Automatic).MakeFullPath(new ParsedPath(baseDir, PathType.Directory));
            else
                pp = new ParsedPath(path, PathType.Automatic).MakeFullPath();
        
            Assert.AreEqual(machine, pp.Machine);
            Assert.AreEqual(share, pp.Share);
            Assert.AreEqual(drive, pp.Drive);
            Assert.AreEqual(directory, pp.Directory);
            Assert.AreEqual(file, pp.File);
            Assert.AreEqual(extension, pp.Extension);
        }
        
        private void AssertBadPathFull(
            string path, 
            string baseDir)
        {
            try
            {
                ParsedPath pp = new ParsedPath(path, PathType.Automatic).MakeFullPath(
                    baseDir == null ? null : new ParsedPath(baseDir, PathType.Directory));
                Assert.Fail("Badly formed path not caught");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }
        }
        #endregion

        #region Assert path directory with MakeRelativePath()
        private void AssertPathPartsRelative(
            string path,
            string basePath,
            string directory)
        {
            ParsedPath pp = new ParsedPath(path, PathType.Automatic).MakeRelativePath(new ParsedPath(basePath, PathType.Automatic));
            Assert.AreEqual(directory, pp.Directory);
        }

        private void AssertBadPathPartsRelative(
            string path,
            string basePath)
        {
            try
            {
                ParsedPath pp = new ParsedPath(path, PathType.Automatic).MakeRelativePath(new ParsedPath(basePath, PathType.Automatic));
                Assert.Fail("MakeRelativePath succeeded and should have failed");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }
        }
        #endregion

        #region Assert path directory with MakeParentPath()
        private void AssertParentPath(
            string path,
            int level,
            string baseDir,
            string machine,
            string share,
            string drive,
            string directory,
            string file,
            string extension)
        {
            ParsedPath pp;
            
            // Test out specific entry points based on the values passed in
            if (level < -1)
            {
                if (baseDir != null)
                    pp = new ParsedPath(path, PathType.Automatic).MakeParentPath(level, new ParsedPath(baseDir, PathType.Directory));
                else
                    pp = new ParsedPath(path, PathType.Automatic).MakeParentPath(level);
            
                if (pp == null)
                {
                    Assert.IsNull(directory, "Expected result was not null");
                    return;
                }
            }
            else
            {
                if (baseDir != null)
                    pp = new ParsedPath(path, PathType.Automatic).MakeParentPath(new ParsedPath(baseDir, PathType.Directory));
                else
                    pp = new ParsedPath(path, PathType.Automatic).MakeParentPath();

                if (pp == null)
                {
                    Assert.IsNull(directory, "Expected result was not null");
                    return;
                }
            }
                    
            Assert.AreEqual(machine, pp.Machine);
            Assert.AreEqual(share, pp.Share);
            Assert.AreEqual(drive, pp.Drive);
            Assert.AreEqual(directory, pp.Directory);
            Assert.AreEqual(file, pp.File);
            Assert.AreEqual(extension, pp.Extension);
        }

        private void AssertBadParentPath(
            string path,
            int level)
        {
            try
            {
                ParsedPath pp;
                
                if (level <= -1 || level > 0)
                    pp = new ParsedPath(path, PathType.Automatic).MakeParentPath(level);
                else
                    pp = new ParsedPath(path, PathType.Automatic).MakeParentPath();
                    
                Assert.Fail("Get parent succeeded and should have failed");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }
        }
        #endregion

        [TestMethod] public void TestPathTypes()
        {
            Assert.IsTrue(new ParsedPath(@"c:\temp\", PathType.Automatic).IsDirectory);
            Assert.IsFalse(new ParsedPath(@"c:\temp", PathType.Automatic).IsDirectory);
            Assert.IsTrue(new ParsedPath(@"\\machine\share\foo", PathType.Automatic).HasUnc);
            Assert.IsFalse(new ParsedPath(@"c:\foo", PathType.Automatic).HasUnc);
            Assert.IsTrue(new ParsedPath(@"\\machine\share\foo\*.t?t", PathType.Automatic).HasWildcards);
            Assert.IsFalse(new ParsedPath(@"\\machine\share\foo\foo.txt", PathType.Automatic).HasWildcards);
            Assert.IsTrue(new ParsedPath(@"\\machine\share\foo\test.txt", PathType.Automatic).HasVolume);
            Assert.IsFalse(new ParsedPath(@"foo\test.txt", PathType.Automatic).HasVolume);
            Assert.IsTrue(new ParsedPath(@"C:\share\foo\", PathType.Automatic).HasDrive);
            Assert.IsFalse(new ParsedPath(@"\\machine\share\foo\", PathType.Automatic).HasDrive);
            Assert.IsTrue(new ParsedPath(@"C:\share\foo\..\..\thing.txt", PathType.Automatic).IsRelativePath);
            Assert.IsTrue(new ParsedPath(@"C:\share\foo\...\thing.txt", PathType.Automatic).IsRelativePath);
            Assert.IsTrue(new ParsedPath(@"...\thing.txt", PathType.Automatic).IsRelativePath);
            Assert.IsFalse(new ParsedPath(@"\\machine\share\foo\thing.txt", PathType.Automatic).IsRelativePath);
            Assert.IsTrue(new ParsedPath(@"C:\share\foo\thing.txt", PathType.Automatic).IsFullPath);
            Assert.IsFalse(new ParsedPath(@"\thing.txt", PathType.Automatic).IsFullPath);
            Assert.IsFalse(new ParsedPath(@"c:\a\..\thing.txt", PathType.Automatic).IsFullPath);
        }

        [TestMethod] public void TestSubDirectories()
        {
            Assert.AreEqual(4, new ParsedPath(@"c:\a\b\c\", PathType.Automatic).DirectoryDepth);
            Assert.AreEqual(4, new ParsedPath(@"\\machine\share\a\b\c\", PathType.Automatic).DirectoryDepth);
            
            string[] subDirs;
            
            subDirs = new ParsedPath(@"c:\a\b\c\", PathType.Directory).SubDirectories;
            
            Assert.AreEqual(4, subDirs.Length);
            Assert.AreEqual(Path.DirectorySeparatorChar.ToString(), subDirs[0]);
            Assert.AreEqual("c", subDirs[3]);
            
            subDirs = new ParsedPath(@"c:\", PathType.Directory).SubDirectories;
            
            Assert.AreEqual(1, subDirs.Length);
            Assert.AreEqual(Path.DirectorySeparatorChar.ToString(), subDirs[0]);
        }

		[TestMethod] public void TestAppend()
		{
            ParsedPath pp1 = new ParsedPath(@"c:\blah\blah", PathType.Directory);
            ParsedPath ppCombine = pp1.Append("file.txt", PathType.File);

            Assert.AreEqual(@"c:\blah\blah\file.txt", ppCombine);

            pp1 = new ParsedPath(@"c:\blah\blah", PathType.Directory);
            Assert.Throws(delegate { ppCombine = pp1.Append(@"\blah\file.txt", PathType.File); }, typeof(ArgumentException));

            pp1 = new ParsedPath(@"c:\blah\blah", PathType.Directory);
            ppCombine = pp1.Append(@"blah\file.txt", PathType.File);

            Assert.AreEqual(@"c:\blah\blah\blah\file.txt", ppCombine);
        }
    }
}
