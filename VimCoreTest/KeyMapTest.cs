﻿using System.Linq;
using NUnit.Framework;
using Vim.Extensions;

namespace Vim.UnitTest
{
    public abstract class KeyMapTest
    {
        protected IVimGlobalSettings _globalSettings;
        protected IKeyMap _map;
        internal KeyMap _mapRaw;

        [SetUp]
        public void SetUp()
        {
            _globalSettings = new GlobalSettings();
            _mapRaw = new KeyMap(_globalSettings);
            _map = _mapRaw;
        }

        protected void AssertMapping(string lhs, string expected)
        {
            var ret = _map.GetKeyMappingResult(lhs, KeyRemapMode.Normal);
            Assert.IsTrue(ret.IsMapped);
            Assert.AreEqual(KeyInputSetUtil.OfString(lhs), ret.AsMapped().Item);
        }

        [TestFixture]
        public sealed class MapWithNoRemap : KeyMapTest
        {
            [Test]
            public void AlphaToAlpha()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "b", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping('a', KeyRemapMode.Normal).Single();
                Assert.AreEqual(KeyInputUtil.CharToKeyInput('b'), ret);
            }

            [Test]
            public void AlphaToDigit()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "1", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping('a', KeyRemapMode.Normal).Single();
                Assert.AreEqual(KeyInputUtil.CharToKeyInput('1'), ret);
            }

            [Test]
            public void ManyAlphaToSingle()
            {
                Assert.IsTrue(_map.MapWithNoRemap("ab", "b", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping("ab", KeyRemapMode.Normal).Single();
                Assert.AreEqual(KeyInputUtil.CharToKeyInput('b'), ret);
            }

            [Test]
            public void SymbolToSymbol()
            {
                Assert.IsTrue(_map.MapWithNoRemap("&", "!", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping('&', KeyRemapMode.Normal).Single();
                Assert.AreEqual(KeyInputUtil.CharToKeyInput('!'), ret);
            }

            [Test]
            public void OneAlphaToTwo()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "bc", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping('a', KeyRemapMode.Normal).ToList();
                Assert.AreEqual(2, ret.Count);
                Assert.AreEqual('b', ret[0].Char);
                Assert.AreEqual('c', ret[1].Char);
            }

            [Test]
            public void OneAlphaToThree()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "bcd", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal).ToList();
                Assert.AreEqual(3, ret.Count);
                Assert.AreEqual('b', ret[0].Char);
                Assert.AreEqual('c', ret[1].Char);
                Assert.AreEqual('d', ret[2].Char);
            }

            [Test]
            public void DontRemapEmptyString()
            {
                Assert.IsFalse(_map.MapWithNoRemap("a", "", KeyRemapMode.Normal));
            }

            [Test]
            public void ShiftPromotesAlpha()
            {
                Assert.IsTrue(_map.MapWithNoRemap("<S-a>", "#", KeyRemapMode.Normal));
                Assert.IsTrue(_map.GetKeyMappingResult('a', KeyRemapMode.Normal).IsNoMapping);
                Assert.IsTrue(_map.GetKeyMappingResult('A', KeyRemapMode.Normal).IsMapped);
            }

            [Test]
            public void ShiftWithUpperAlphaIsJustUpperAlpha()
            {
                Assert.IsTrue(_map.MapWithNoRemap("<S-A>", "#", KeyRemapMode.Normal));
                Assert.IsTrue(_map.GetKeyMappingResult('a', KeyRemapMode.Normal).IsNoMapping);
                Assert.IsTrue(_map.GetKeyMappingResult('A', KeyRemapMode.Normal).IsMapped);
            }

            [Test]
            public void ShiftSymbolDoesNotChangeChar()
            {
                Assert.IsTrue(_map.MapWithNoRemap("<S-#>", "pound", KeyRemapMode.Normal));

                var keyInput = KeyInputUtil.CharToKeyInput('#');
                Assert.IsTrue(_map.GetKeyMappingResult(keyInput, KeyRemapMode.Normal).IsNoMapping);
                Assert.IsTrue(_map.GetKeyMappingResult(KeyInputUtil.ChangeKeyModifiersDangerous(keyInput, KeyModifiers.Shift), KeyRemapMode.Normal).IsMapped);
            }

            [Test]
            public void LessThanChar()
            {
                Assert.IsTrue(_map.MapWithNoRemap("<", "pound", KeyRemapMode.Normal));
            }

            /// <summary>
            /// By default the '\' character isn't special in key mappings.  It's treated like any
            /// other character.  It only achieves special meaning when 'B' is excluded from the 
            /// 'cpoptions' setting
            /// </summary>
            [Test]
            public void EscapeLessThanSymbol()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", @"\<Home>", KeyRemapMode.Normal));
                var result = _map.GetKeyMappingResult("a", KeyRemapMode.Normal);
                Assert.AreEqual(KeyInputSetUtil.OfVimKeyArray(VimKey.Backslash, VimKey.Home), result.AsMapped().Item);
            }

            [Test]
            public void HandleLessThanEscapeLiteral()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "<lt>lt>", KeyRemapMode.Normal));
                var result = _map.GetKeyMappingResult("a", KeyRemapMode.Normal);
                Assert.AreEqual(KeyInputSetUtil.OfString("<lt>"), result.AsMapped().Item);
            }

            [Test]
            public void ControlAlphaIsCaseInsensitive()
            {
                Assert.IsTrue(_map.MapWithNoRemap("<C-a>", "1", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithNoRemap("<C-A>", "2", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping("<C-a>", KeyRemapMode.Normal).Single();
                Assert.AreEqual(KeyInputUtil.CharToKeyInput('2'), ret);
                ret = _map.GetKeyMapping("<C-A>", KeyRemapMode.Normal).Single();
                Assert.AreEqual(KeyInputUtil.CharToKeyInput('2'), ret);
            }

            [Test]
            public void AltAlphaIsCaseSensitive()
            {
                Assert.IsTrue(_map.MapWithNoRemap("<A-a>", "1", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithNoRemap("<A-A>", "2", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping("<A-a>", KeyRemapMode.Normal).Single();
                Assert.AreEqual(KeyInputUtil.CharToKeyInput('1'), ret);
                ret = _map.GetKeyMapping("<A-A>", KeyRemapMode.Normal).Single();
                Assert.AreEqual(KeyInputUtil.CharToKeyInput('2'), ret);
            }

            /// <summary>
            /// When two mappnigs have the same prefix then they are ambiguous and require a
            /// tie breaker input
            /// </summary>
            [Test]
            public void Ambiguous()
            {
                Assert.IsTrue(_map.MapWithNoRemap("aa", "foo", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithNoRemap("aaa", "bar", KeyRemapMode.Normal));
                var ret = _map.GetKeyMappingResult("aa", KeyRemapMode.Normal);
                Assert.IsTrue(ret.IsNeedsMoreInput);
            }

            /// <summary>
            /// Resloving the ambiguity should cause both the original plus the next input to be 
            /// returned
            /// </summary>
            [Test]
            public void Ambiguous_ResolveShorter()
            {
                Assert.IsTrue(_map.MapWithNoRemap("aa", "foo", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithNoRemap("aaa", "bar", KeyRemapMode.Normal));
                var ret = _map.GetKeyMappingResult("aab", KeyRemapMode.Normal);
                Assert.IsTrue(ret.IsMapped);
                Assert.AreEqual(KeyInputSetUtil.OfString("foob"), ret.AsMapped().Item);
            }

            [Test]
            public void Ambiguous_ResolveLonger()
            {
                Assert.IsTrue(_map.MapWithNoRemap("aa", "foo", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithNoRemap("aaa", "bar", KeyRemapMode.Normal));
                var ret = _map.GetKeyMappingResult("aaa", KeyRemapMode.Normal);
                Assert.IsTrue(ret.IsMapped);
                Assert.AreEqual(KeyInputSetUtil.OfString("bar"), ret.AsMapped().Item);
            }
        }

        [TestFixture]
        public sealed class MapWithRemap : KeyMapTest
        {
            [Test]
            public void MapWithRemap1()
            {
                Assert.IsTrue(_map.MapWithRemap("a", "b", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal).Single();
                Assert.AreEqual('b', ret.Char);
            }

            [Test]
            public void MapWithRemap2()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "bcd", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal).ToList();
                Assert.AreEqual(3, ret.Count);
                Assert.AreEqual('b', ret[0].Char);
                Assert.AreEqual('c', ret[1].Char);
                Assert.AreEqual('d', ret[2].Char);
            }

            [Test]
            public void MapWithRemap3()
            {
                Assert.IsTrue(_map.MapWithRemap("a", "b", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithRemap("b", "c", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal).Single();
                Assert.AreEqual('c', ret.Char);
            }

            [Test]
            public void MapWithRemap4()
            {
                Assert.IsTrue(_map.MapWithRemap("a", "bc", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithRemap("b", "d", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal).ToList();
                Assert.AreEqual(2, ret.Count);
                Assert.AreEqual('d', ret[0].Char);
                Assert.AreEqual('c', ret[1].Char);
            }

            /// <summary>
            /// When expanding a given key mapping that particular key should not be scanned for remapping 
            /// on the RHS.  For example ":map j gj" is not recursive.  
            /// </summary>
            [Test]
            public void MapWithRemap_SameKey()
            {
                Assert.IsTrue(_map.MapWithRemap("j", "gj", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping('j', KeyRemapMode.Normal).ToList();
                CollectionAssert.AreEquivalent(
                    new[] { 'g', 'j' },
                    ret.Select(x => x.Char).ToList());
            }

            /// <summary>
            /// When expanding a given key mapping that particular key should not be scanned for remapping 
            /// on the RHS.  For example ":map j gj" is not recursive.  
            /// </summary>
            [Test]
            public void MapWithRemap_SameKeyPair()
            {
                Assert.IsTrue(_map.MapWithRemap("jk", "jkg", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping("jk", KeyRemapMode.Normal).ToList();
                CollectionAssert.AreEquivalent(
                    new[] { 'j', 'k', 'g' },
                    ret.Select(x => x.Char).ToList());
            }

            [Test]
            public void MapWithRemap_HandleCommandKey()
            {
                Assert.IsTrue(_map.MapWithRemap("<D-k>", "gk", KeyRemapMode.Normal));
                var ki = new KeyInput(VimKey.LowerK, KeyModifiers.Command, FSharpOption.Create('k'));
                var kiSet = KeyInputSet.NewOneKeyInput(ki);
                var ret = _map.GetKeyMapping(kiSet, KeyRemapMode.Normal);
                Assert.IsTrue(ret.IsMapped);
                var all = ret.AsMapped().Item.KeyInputs.Select(x => x.Char).ToList();
                Assert.AreEqual('g', all[0]);
                Assert.AreEqual('k', all[1]);
            }
        }

        [TestFixture]
        public sealed class Misc : KeyMapTest
        {
            [Test, Description("Recursive mappings should not follow the recursion here")]
            public void GetKeyMapping1()
            {
                Assert.IsTrue(_map.MapWithRemap("a", "b", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithRemap("b", "a", KeyRemapMode.Normal));
                var ret = _map.GetKeyMapping(KeyInputSetUtil.OfChar('a'), KeyRemapMode.Normal);
                Assert.IsTrue(ret.IsRecursive);
            }

            [Test]
            public void GetKeyMappingResult1()
            {
                Assert.IsTrue(_map.MapWithRemap("a", "b", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithRemap("b", "a", KeyRemapMode.Normal));
                var ret = _map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal);
                Assert.IsTrue(ret.IsRecursive);
            }

            [Test]
            public void GetKeyMappingResult2()
            {
                var ret = _map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('b'), KeyRemapMode.Normal);
                Assert.IsTrue(ret.IsNoMapping);
            }

            [Test]
            public void GetKeyMapppingResult3()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "b", KeyRemapMode.Normal));
                var res = _map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal);
                Assert.IsTrue(res.IsMapped);
                Assert.AreEqual('b', res.AsMapped().Item.KeyInputs.Single().Char);
            }

            [Test]
            public void GetKeyMappingResult4()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "bc", KeyRemapMode.Normal));
                var res = _map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal);
                Assert.IsTrue(res.IsMapped);
                var list = res.AsMapped().Item.KeyInputs.ToList();
                Assert.AreEqual(2, list.Count);
                Assert.AreEqual('b', list[0].Char);
                Assert.AreEqual('c', list[1].Char);
            }

            [Test]
            public void GetKeyMappingResult5()
            {
                Assert.IsTrue(_map.MapWithNoRemap("aa", "b", KeyRemapMode.Normal));
                var res = _map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal);
                Assert.IsTrue(res.IsNeedsMoreInput);
            }

            [Test]
            public void Clear1()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "b", KeyRemapMode.Normal));
                _map.Clear(KeyRemapMode.Normal);
                Assert.IsTrue(_map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal).IsNoMapping);
            }

            [Test, Description("Only clear the specified mode")]
            public void Clear2()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "b", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithNoRemap("a", "b", KeyRemapMode.Insert));
                _map.Clear(KeyRemapMode.Normal);
                var res = _map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Insert);
                Assert.IsTrue(res.IsMapped);
                Assert.AreEqual('b', res.AsMapped().Item.KeyInputs.Single().Char);
            }

            [Test]
            public void ClearAll()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "b", KeyRemapMode.Normal));
                Assert.IsTrue(_map.MapWithNoRemap("a", "b", KeyRemapMode.Insert));
                _map.ClearAll();
                var res = _map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Insert);
                Assert.IsTrue(res.IsNoMapping);
                res = _map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal);
                Assert.IsTrue(res.IsNoMapping);

            }

            [Test]
            public void Unmap1()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "b", KeyRemapMode.Normal));
                Assert.IsTrue(_map.Unmap("a", KeyRemapMode.Normal));
                Assert.IsTrue(_map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal).IsNoMapping);
            }

            [Test]
            public void Unmap2()
            {
                Assert.IsTrue(_map.MapWithNoRemap("a", "b", KeyRemapMode.Normal));
                Assert.IsFalse(_map.Unmap("a", KeyRemapMode.Insert));
                Assert.IsTrue(_map.GetKeyMappingResult(KeyInputUtil.CharToKeyInput('a'), KeyRemapMode.Normal).IsMapped);
            }

            /// <summary>
            /// Straight forward unmap of via the mapping instead of the key 
            /// </summary>
            [Test]
            public void UnmapByMapping_Simple()
            {
                Assert.IsTrue(_map.MapWithNoRemap("cat", "dog", KeyRemapMode.Insert));
                Assert.IsTrue(_map.UnmapByMapping("dog", KeyRemapMode.Insert));
                Assert.IsTrue(_map.GetKeyMappingResult("cat", KeyRemapMode.Insert).IsNoMapping);
            }

            /// <summary>
            /// Don't allow the unmapping by the key
            /// </summary>
            [Test]
            public void UnmapByMapping_Bad()
            {
                Assert.IsTrue(_map.MapWithNoRemap("cat", "dog", KeyRemapMode.Insert));
                Assert.IsFalse(_map.UnmapByMapping("cat", KeyRemapMode.Insert));
            }

            [Test]
            public void GetKeyMappingResultFromMultiple1()
            {
                _map.MapWithNoRemap("aa", "b", KeyRemapMode.Normal);

                var input = "aa".Select(KeyInputUtil.CharToKeyInput).ToFSharpList();
                var res = _map.GetKeyMapping(KeyInputSet.NewManyKeyInputs(input), KeyRemapMode.Normal);
                Assert.AreEqual('b', res.AsMapped().Item.KeyInputs.Single().Char);
            }

            [Test]
            public void GetKeyMappingResultFromMultiple2()
            {
                _map.MapWithNoRemap("aa", "b", KeyRemapMode.Normal);

                var input = "a".Select(KeyInputUtil.CharToKeyInput).ToFSharpList();
                var res = _map.GetKeyMapping(KeyInputSet.NewManyKeyInputs(input), KeyRemapMode.Normal);
                Assert.IsTrue(res.IsNeedsMoreInput);
            }

            [Test]
            public void Issue328()
            {
                Assert.IsTrue(_map.MapWithNoRemap("<S-SPACE>", "<ESC>", KeyRemapMode.Insert));
                var res = _map.GetKeyMapping(KeyInputUtil.ApplyModifiersToVimKey(VimKey.Space, KeyModifiers.Shift), KeyRemapMode.Insert);
                Assert.AreEqual(KeyInputUtil.EscapeKey, res.Single());
            }
        }
    }
}
