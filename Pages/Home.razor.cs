using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Blazored.LocalStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyWordAppWeb.Pages // ⚠️ 改成你的项目名.Pages
{
    public partial class Home
    {
        [Inject] public ILocalStorageService LocalStorage { get; set; }
        [Inject] public IJSRuntime JS { get; set; }
        [Inject] public HttpClient Http { get; set; }

        private List<Word> wordList = new();
        private int currentIndex = 0;
        private bool showMeaning = false; // 是否显示中文（是否已作答）
        private bool isLoading = true;
        private bool isKnownFlag = true;  // 记录当前词是认识还是不认识

        // 核心：复习队列和计数器
        private List<Word> reviewQueue = new();
        private int reviewCounter = 0; // 记录背了几个词，用来定时插入复习词

        private Word currentWord => wordList.Count > 0 ? wordList[currentIndex] : new Word();

        protected override async Task OnInitializedAsync()
        {
            var savedWords = await LocalStorage.GetItemAsync<List<Word>>("my_words");

            if (savedWords == null || savedWords.Count == 0)
            {
                var text = await Http.GetStringAsync("words.txt");
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                wordList = new List<Word>();
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        wordList.Add(new Word { Id = wordList.Count + 1, Spelling = parts[0].Trim(), Meaning = parts[1].Trim() });
                    }
                }
                await LocalStorage.SetItemAsync("my_words", wordList);
            }
            else
            {
                wordList = savedWords;
            }

            var lastIndex = await LocalStorage.GetItemAsync<int>("last_index");
            var isShuffle = await JS.InvokeAsync<bool>("confirm", "点击【确定】重新随机打乱顺序\n点击【取消】接着上次的进度继续");

            if (isShuffle)
            {
                var rnd = new Random();
                wordList = wordList.OrderBy(x => rnd.Next()).ToList();
                currentIndex = 0;
                await LocalStorage.SetItemAsync("last_index", 0);
            }
            else
            {
                if (lastIndex > 0 && lastIndex < wordList.Count)
                {
                    currentIndex = lastIndex;
                }
            }

            isLoading = false;
        }

        private async Task SaveProgress()
        {
            await LocalStorage.SetItemAsync("last_index", currentIndex);
        }

        private async Task SpeakWord()
        {
            await JS.InvokeVoidAsync("speak", currentWord.Spelling);
        }

        // 1. 点击【不认识】：自动显示词义，自动发音，加入复习队列
        private async Task DontKnowWord()
        {
            showMeaning = true;
            isKnownFlag = false;
            AddToReviewQueue(currentWord); // 加入复习队列
            await SpeakWord();
            StateHasChanged();
        }

        // 2. 点击【认识】：自动显示词义，自动发音，暂不加入队列
        private async Task KnowWord()
        {
            showMeaning = true;
            isKnownFlag = true;
            await SpeakWord();
            StateHasChanged();
        }

        // 3. 点击【认错了】：纠正“认识”的误判，加入复习队列
        private async Task MistakenWord()
        {
            AddToReviewQueue(currentWord);
            // 纠正后，自动执行下一个词的逻辑
            await NextWord();
        }

        // 4. 点击【下一个】
        private async Task NextWord()
        {
            // 每背 3 个词，复习队列里的词就自动插到后面
            reviewCounter++;
            if (reviewCounter >= 3 && reviewQueue.Any())
            {
                var wordToReview = reviewQueue.First();
                reviewQueue.RemoveAt(0);
                // 插到当前索引后面第 2 个位置（保证不是马上出现，但过一会儿会出现）
                int insertIndex = Math.Min(wordList.Count, currentIndex + 2);
                wordList.Insert(insertIndex, wordToReview);
                reviewCounter = 0; // 重置计数器
            }

            if (currentIndex < wordList.Count - 1)
            {
                currentIndex++;
                showMeaning = false;
                await SaveProgress();
            }
            else
            {
                // 如果背到最后还有复习词没消化完，回退到队列重新开始
                if (reviewQueue.Any())
                {
                    var wordToReview = reviewQueue.First();
                    reviewQueue.RemoveAt(0);
                    wordList.Add(wordToReview);
                }
                await JS.InvokeVoidAsync("alert", "今天的单词背完啦！🎉");
            }
            StateHasChanged();
        }

        // 辅助方法：加入复习队列
        private void AddToReviewQueue(Word word)
        {
            // 去重，防止同一个词重复加入
            if (!reviewQueue.Any(w => w.Id == word.Id))
            {
                reviewQueue.Add(word);
            }
        }

        // 5. 点击【重新随机】
        private async Task ShuffleWords()
        {
            var rnd = new Random();
            wordList = wordList.OrderBy(x => rnd.Next()).ToList();
            currentIndex = 0;
            showMeaning = false;
            await SaveProgress();
            await JS.InvokeVoidAsync("alert", "已重新随机打乱！");
        }
    }
}