﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PROJ3004
{
    public class Gameboard
    {
		public List<Adventure> adventureDeck  = new List<Adventure>();//125
		public List<Adventure> discardAdventureDeck = new List<Adventure> ();

		public List<Story> storyDeck = new List<Story>();//28
		public List<Story> discardStoryDeck = new List<Story> ();

		public List<Player> playerList = new List<Player> ();
		public int numPlayers = 0;
		public bool gameOver = false;
		public int playerTurn = 1;

		public int QuestBonusShields = 0;

        public static void Main(string[] args) //Move all creation to a seperate function, only keep main control loop in here moving forward!
        {
			Gameboard play = new Gameboard ();

			play.createDeck ();	//could move everything except play.runGame() into runGame()...think about this...yes I should do this moving forward IMPLEMENT THIS CHANGE
			play.shuffle ("adventureDeck");
			play.shuffle ("storyDeck");

			play.initializePlayers (); //make the players
			play.deal (); //Giving everyone 12 adventure cards

			for (int i = 0; i < play.playerList.Count; i++) {//FOR TESTING ONLY, trying to figure wtf is going on
				Player p = play. playerList[i];
				Console.WriteLine (p.getName());
				Console.WriteLine ("-----------------");
				for (int x = 0; x < p.cardsInHand.Count; x++) {
					Console.WriteLine (p.cardsInHand[x].GetName());
				}
				Console.WriteLine ("-------------");
			}

			play.runGame ();


        }

		/*Control Loop */ 
		public void runGame(){
			while (!gameOver) {
				//First thing that needs to be done, P1 has to draw a story card
				Story storyCard = (Story) draw("Story");
				if (storyCard is Quest) {
					isQuest ((Quest)storyCard);
					for (int x = 0; x < playerList.Count; x++) {//FOR TESTING ONLY, making sure the right person got the number of shields
						Console.WriteLine (playerList[x].shields);
						Console.WriteLine (playerList[x].cardsInHand.Count);
					}
					gameOver = rankUp ();
					whosTurn ();
				} else if (storyCard is Tournament) {
					isTournament ((Tournament)storyCard);
					for (int x = 0; x < playerList.Count; x++) {//FOR TESTING ONLY, making sure the right person got the number of shields
						Console.WriteLine (playerList[x].shields);
						Console.WriteLine (playerList[x].cardsInHand.Count);
					}
					gameOver = rankUp ();
					whosTurn (); //This is now working
				} else if (storyCard is Event) {
					isEvent ((Event)storyCard);
					gameOver = rankUp ();
					whosTurn ();
				} else {
					Console.WriteLine ("Critical error 9001 in card type, this should never happen");
				}
					
			}
			Console.WriteLine ("Someone has won...expand on this");
		}

		public int whosTurn(){
			if (playerTurn > numPlayers) {
				playerTurn = 1;
			}
			Console.WriteLine ("Entered who's turn: " + playerTurn);
			return playerTurn++;
		}

		public bool rankUp(){
			bool hasWon = false;
			for (int i = 0; i < playerList.Count; i++) {
				Player p = playerList [i];
				if (p.rank == "Squire" && p.shields >= 5) {
					p.rank = "Knight";
					p.shields -= 5;
				}
				if (p.rank == "Knight" && p.shields >= 7) {
					p.rank = "Champion Knight";
					p.shields -= 7;
				}
				if (p.rank == "Champion Knight" && p.shields >= 10) {
					//Player has won and become a knight of the round
					hasWon = true;
				}
			}
			return hasWon;
		}

		public void isQuest(Quest storyCard){
			Console.WriteLine ("A new quest! " +  storyCard.GetName() + " with " + storyCard.GetNumStages() + " stages.");
			Console.WriteLine ("Linked Foe: " + storyCard.GetLinkedFoe() + " | " + "Linked Ally: " + storyCard.GetLinkedAlly());
			Player host = getHost (storyCard);
			if (host == null) {
				Console.WriteLine ("No available host for " + storyCard.GetName());
				return;
			}	
			Console.WriteLine (host.getName() + " will be hosting. ");
			int tempPlayerTurn = playerTurn;
			List <Pair<Player, int>> questEntry = new List <Pair<Player, int>> ();
			for(int i = 0; i < numPlayers; i++){ //we have a host, now check who wants to play. playerTurn order is maintained regarless of who sponsored,
				//that is, left to right from whos turn it is who drew the story card, skipping the sponsor as we go. Meaning whomever drew the card gets first chance
				//at being sponsor, and also first change to play if he declines to be the sponsor
				if(tempPlayerTurn > numPlayers){
					tempPlayerTurn = 1;
				}
				if(playerList[tempPlayerTurn -1] != host){
					Console.WriteLine (playerList[tempPlayerTurn - 1].getName() + " would you like to partake in the Quest?");
					string userInput = Console.ReadLine();
					if(userInput == "y" || userInput == "Y"){
						questEntry.Add(new Pair<Player,int> (playerList[tempPlayerTurn - 1], 0));
					}
				}
				tempPlayerTurn++;
			}
			if (questEntry.Count < 1) {
				Console.WriteLine ("No one was brave enough to take the " + storyCard.GetName());
				return;
			}
			
			//Incrementing through questEntry now will present the correct order of people to play
			int hostCardsBeforeQuest = host.cardsInHand.Count;
			Console.WriteLine ("Host select appropriate cards for the quest IN ORDER PROMPTED or they will be auto-selected for you.");
			for (int y = 0; y < host.cardsInHand.Count; y++) {
				Console.WriteLine ("Card #" + (y + 1) + ": " + host.cardsInHand [y].GetName () + " with " + host.cardsInHand [y].GetBattlePoints () + " battle points.");
			}
			//string hostInput = Console.ReadLine ();
			//string[] values = hostInput.Split (',');
			//List<Adventure> questStages = isValidQuest (values, host, storyCard.GetNumStages());
			Console.WriteLine ("Host stages currently always being auto selected.");
			List<Pair<List<Adventure>,int>> questStages = autoSelectStages (host, storyCard.GetNumStages(), storyCard.GetLinkedFoe());

			for (int i = 0; i < questStages.Count; i++) {
				Adventure a = questStages [i].Item1 [0]; //because the lists either lead with a Foe or Test
				if (a is Foe) {
					Console.WriteLine ("Stage " + (i+1) + " is a Foe");
					for (int x = 0; x < questEntry.Count; x++) { //showing player the cards they have, making them choose which cards they want to play
						Player player = questEntry[x].Item1;
						string[] values = questPreParse (player);
						questEntry[x].Item2 = validCards (values, player, "yes");
						questEntry [x].Item2 += linkedAllyBoost (storyCard.GetLinkedAlly (), player);
					}
					passedFoeStage (questEntry, questStages [i].Item2, i);
				} else if (a is Test) {
					Console.WriteLine ("Stage " + (i+1) + " is a Test");
					biddingWar ((Test)a, questEntry, storyCard.GetName());
				} else {
					Console.WriteLine ("Critical error in isQuest, quest stages array. 9001. This should never happen!");
					Console.ReadLine ();
				}
					
				stripCards (); //can;t have more than 12 at point after a stage, applies to host as well (though host having over 12 seems impossible)
				if (questEntry.Count < 1) {
					Console.WriteLine ("All players eliminated");
					break;
				}
			}

			int hostUsedCards = storyCard.GetNumStages();
			foreach (var p in questStages) {//get rid of all the cards the host used
				List<Adventure> temp = p.Item1;
				hostUsedCards += temp.Count;
				removeHostCards (temp, host);

			}

			for (int i = 0; i < hostUsedCards; i++) {//allow the host to draw cards equivalent to how many they used + num stages in quest
				host.cardsInHand.Add((Adventure) draw("Adventure"));
			}

			discardAfterTest (null, -1, -1, " immeadiately after");//can use this method to make the host get rid of cards over limit
			stripCards();

			foreach (var p in questEntry) {//whomever made it through gets shields equivalent to the number of stages
				p.Item1.shields += storyCard.GetNumStages ();
			}

			foreach (var p in playerList) { //end of quest, make sure to reset this
				p.amourOnQuest = false;
			}
		}

		public void removeHostCards(List<Adventure> hostCardsUsed, Player host){
			for(int i = host.cardsInHand.Count - 1; i > -1; i--) {
				var b = from Adventure a in hostCardsUsed
						where ReferenceEquals (a, host.cardsInHand[i])
				        select a;
				if (b != null && b.Count() > 0) {
					try {
						var n = b.First ();
						Console.WriteLine ("Host used " + n.GetName () + " in quest. Getting rid of it now.");
						host.discardPile.Add (host.cardsInHand [i]);
						host.cardsInHand.RemoveAt (i);
					} catch (Exception e) {
						Console.WriteLine (e.ToString () + " There should be no circumstance where this ever happens");
						Console.ReadLine ();
					}
				}
			
			}
		}

		public void biddingWar(Test test, List<Pair<Player,int>> questEntry, string questName){
			int previousMaxBid = test.GetMinBids ();
			if (questEntry.Count == 1 && 3 > previousMaxBid)
				previousMaxBid = 3;//if there's only one player in the quest, the min bids of a test is 3
			if (questName == "Search for the Questing Beast" && test.GetName () == "Test of the Questing Beast")
				previousMaxBid = 4;

			List <Pair<int,int>> bidMath = new List<Pair<int,int>> (); //first for player max bid, second for free bids

			for (int i = 0; i < questEntry.Count; i++) {
				Player p = questEntry [i].Item1;
				p.cardsInHand.Add ((Adventure)draw ("Adventure")); //everyone partaking gets an additional adventure card before each stage
				int playerMaxBid = p.cardsInHand.Count;
				int freeBids = 0;

				if (questName == "Search for the Questing Beast") {
					var results = from Ally ally in p.alliesInPlay
					              where ally.GetName () == "King Pellinore"
					              select ally;
					var pelly = results.FirstOrDefault ();
					if (pelly != null) {
						playerMaxBid += 4;
						freeBids += 4;
					}
				}

				var couple = from Ally ally in p.alliesInPlay
				             where ally.GetName () == "Queen Iseult" || ally.GetName () == "Sir Tristan"
				             select ally;
				if (couple != null && couple.Count () == 2) { //Need both Sir Tristan and Queen Iseult to be together to her bid boost
					playerMaxBid += 2;
					freeBids += 2;
				}

				foreach (var a in p.alliesInPlay) {
					playerMaxBid += a.GetBids ();
					freeBids += a.GetBids ();
				}

				if (p.amourOnQuest) {
					playerMaxBid += 1;
					freeBids += 1;
				}

				Console.WriteLine (p.getName() + " has max bids of " + playerMaxBid + ". (including " + freeBids + " free bids");
				bidMath.Add (new Pair<int,int> (playerMaxBid, freeBids));
			}

			for (int i = questEntry.Count - 1; i > -1; i--) {
				if (bidMath [i].Item1 < previousMaxBid) {
					Console.WriteLine (questEntry[i].Item1.getName() + " has been eliminated for insufficient bid ability.");
					questEntry.RemoveAt (i);
					bidMath.RemoveAt (i);
				}
			}

			if (questEntry.Count == 1) {
				Console.WriteLine (questEntry [0].Item1.getName () + " do you wish to place the min bid of " + previousMaxBid + " to continue?");
				string userInput = Console.ReadLine ();
				if (!(userInput.ToLower () == "yes" || userInput.ToLower () == "y") || previousMaxBid > bidMath[0].Item1 ) {
					Console.WriteLine ("Okay, then you are eliminated.");
					return;
				}
			}

			while (questEntry.Count > 1) {
				for (int i = 0; i < questEntry.Count; i++) {
					if (questEntry.Count == 1)
						break;

					Console.WriteLine (questEntry[i].Item1.getName() + " you must bid higher than " + previousMaxBid + "\n What is your bid?" + 
					" (Invalid bid will results in disqualification");
					string userInput = Console.ReadLine ();
					int x = -1;
					bool parsed = Int32.TryParse (userInput, out x);
					if (parsed && x > previousMaxBid && x <= bidMath [i].Item1) {
						Console.WriteLine ("Okay " + questEntry[i].Item1.getName() + "you have bid successfully with " + x + " bids.");
						previousMaxBid = x;
					} else {
						Console.WriteLine ("Invalid or not enough bids. " + questEntry[i].Item1.getName() +  " has been eliminated.");
						questEntry.RemoveAt (i);
						bidMath.RemoveAt (i);
						i--;
					}
				}
			}

			if (questEntry.Count < 1) {
				Console.WriteLine ("All players eliminated");
				discardAfterTest (null, -1, -1);
			} else if (questEntry.Count == 1) {
				Console.WriteLine ("Congrats " + questEntry [0].Item1.getName () + " you have passed the test!");
				discardAfterTest (questEntry [0].Item1, previousMaxBid, bidMath [0].Item2);
			} else {
				Console.WriteLine ("Only one player should pass a test, this should never happen! Error 9001");
				Console.ReadLine ();
			}
		}

		public void discardAfterTest(Player testWinner, int winningBidNo, int winnerFreebids, string whenDiscard = " at end of stage"){ //repeated code garbage method. Modify later once everything is working!!
			for (int i = 0; i < numPlayers; i++) {
				Player player = playerList [i];
				if (testWinner == player)
					continue;

				if (player.cardsInHand.Count > Player.MAX_CARDS_HAND) {
					for (int y = 0; y < player.cardsInHand.Count; y++) {
						if (y == 0 && player.cardsInHand.Count > 12)
							Console.WriteLine ("Alert, " + player.getName () + " you have more than 12 cards. Discard to (at least) 12 or be auto discarded" + whenDiscard);
						Console.WriteLine ("Card #" + (y + 1) + ": " + player.cardsInHand [y].GetName () + " with " + player.cardsInHand [y].GetBattlePoints () + " battle points.");
					}

					Console.WriteLine ("Choose the cards you want to discard by card#, seperated by commas. Invalid cards will be ignored.");
					string cardsChosen = Console.ReadLine ();
					string[] values = cardsChosen.Split (',');
					List<int> validatedCards = new List<int> ();
					for (int y = 0; y < values.Length; y++) { //Making sure values that aren't acceptable ints are stripped
						int temp = -1; 
						bool tempBool = int.TryParse (values [y], out temp);
						if (tempBool && temp > 0 && temp <= player.cardsInHand.Count) {
							validatedCards.Add (temp);
						}
					}
					validatedCards.Sort ();

					for (int y = validatedCards.Count - 1; y >= 0; y--) {
						Console.WriteLine (player.getName () + " discarded " + player.cardsInHand [validatedCards [y] - 1].GetName ());
						player.discardPile.Add (player.cardsInHand [validatedCards [y] - 1]);
						player.cardsInHand.RemoveAt (validatedCards [y] - 1);
					}
				}
			}//end outer for


			if (testWinner != null) {
				int twocih = testWinner.cardsInHand.Count;
				Console.WriteLine (testWinner.getName());
				for (int y = 0; y < testWinner.cardsInHand.Count; y++) {
					Console.WriteLine ("Card #" + (y+1) + ": " + testWinner.cardsInHand[y].GetName() + " with " 
						+ testWinner.cardsInHand[y].GetBattlePoints() + " battle points.");
				}
				Console.WriteLine ("Choose the cards you want to discard by card#, seperated by commas. Invalid cards will be ignored.");
				string cardsChosen = Console.ReadLine ();
				string[] values = cardsChosen.Split (',');
				List<int> validatedCards = new List<int> ();
				for (int y = 0; y < values.Length; y++) { //Making sure values that aren't acceptable ints are stripped
					int temp = -1; 
					bool tempBool = int.TryParse (values [y], out temp);
					if (tempBool && temp > 0 && temp <= testWinner.cardsInHand.Count) {
						validatedCards.Add (temp);
					}
				}
				validatedCards.Sort ();

				for (int y = validatedCards.Count - 1; y >= 0; y--) {
					Console.WriteLine (testWinner.getName () + " discarded " + testWinner.cardsInHand [validatedCards [y] - 1].GetName ());
					testWinner.discardPile.Add (testWinner.cardsInHand [validatedCards [y] - 1]);
					testWinner.cardsInHand.RemoveAt (validatedCards [y] - 1);
				}

				while (testWinner.cardsInHand.Count > twocih - (winningBidNo - winnerFreebids)) {//didnt discard enough cards for the bidding
					Console.WriteLine (testWinner.getName () + " force-discarded " + testWinner.cardsInHand [testWinner.cardsInHand.Count - 1].GetName ());
					testWinner.discardPile.Add (testWinner.cardsInHand [testWinner.cardsInHand.Count - 1]);
					testWinner.cardsInHand.RemoveAt (testWinner.cardsInHand.Count - 1);
				}
			}
					
		}

		public string [] questPreParse(Player player){
			player.cardsInHand.Add ((Adventure)draw ("Adventure")); //everyone partaking gets an additional adventure card before each stage
			Console.WriteLine (player.getName());
			for (int y = 0; y < player.cardsInHand.Count; y++) {
				if(y == 0 && player.cardsInHand.Count > 12) 
					Console.WriteLine ("Alert, " + player.getName() + " you have more than 12 cards. Discard to (at least) 12 or be auto discarded at end of stage");
				Console.WriteLine ("Card #" + (y+1) + ": " + player.cardsInHand[y].GetName() + " with " + player.cardsInHand[y].GetBattlePoints() + " battle points.");
			}
				
			Console.WriteLine ("Choose the cards you want to use for the quest by card#, seperated by commas. Invalid cards will be ignored.");
			string cardsChosen = Console.ReadLine ();
			string[] values = cardsChosen.Split (',');
			return values;
		}

		public void passedFoeStage(List<Pair<Player,int>> questEntry, int stageBattlePoints, int stageNo){
			for (int x = questEntry.Count - 1; x > -1; x--) { //check if player has passed the stage succesfully
				var p = questEntry[x];
				Console.WriteLine ("Player battle score: " + p.Item2 + " | " + "Stage battle score " + stageBattlePoints);
				if (p.Item2 >= stageBattlePoints) {
					Console.WriteLine (p.Item1.getName () + "has passed stage " + (stageNo + 1));
					p.Item2 = 0; //reset their bp at the end of each round
				} else {
					Console.WriteLine (p.Item1.getName() + " has been eliminated.");
					questEntry.RemoveAt (x);
				}
			}
		}

		public int linkedAllyBoost(string linkedAlly, Player p){
			int battlePoints = 0;
			//Step 1 check for possible linked ally
			//Step 2 check for both Queen Iseult AND Sir Tristan together
			var results = from Ally ally in p.alliesInPlay
			              where ally.GetName () == linkedAlly
			              select ally;
			var candidate = results.FirstOrDefault ();

			if (candidate != null) {
				if (candidate.GetName () == "Sir Percival") {
					battlePoints += 15; //5 goes to 20 bp on the Search for the Holy Grail Quest
					Console.WriteLine ("Sir Percival boost!!");
				} else if (candidate.GetName () == "Sir Gawain") {
					battlePoints += 10; //10 goes to 20 bp on the Test of the Green Knight Quest
					Console.WriteLine ("Sir Gawain boost!!");
				} else if (candidate.GetName () == "Sir Lancelot") {
					battlePoints += 10; //15 goes to 25 bp on the Quest to Defend the Queen's Honor Quest
					Console.WriteLine ("Sir Lancelot boost!!");
				}
			}
			//Step 1 complete, not for step 2
			var pairedAllies = from Ally ally in p.alliesInPlay
			                   where ally.GetName () == "Queen Iseult" || ally.GetName () == "Sir Tristan"
			                   select ally;

			if (pairedAllies != null && pairedAllies.Count() == 2) {//should be exactly two with both of them in play
				battlePoints += 10; //10 goes to 20 when Queen Iseult is also in play
				Console.WriteLine ("Sir Tristan boost!!");
			}

			return battlePoints;
		}

		public List<Pair<List<Adventure>,int>> autoSelectStages (Player host, int numStages, string linkedFoe){
			List<Pair<List<Adventure>,int>> stages = new List<Pair<List<Adventure>,int>> ();
			List<Pair<Foe,int>> foeList = new List<Pair<Foe,int>> ();
			Test testCard = null;
			//I want every possible weapon combination 
			List<Weapon> weaponList = new List<Weapon>();
			List<List<List<Weapon>>> weaponComboList = new List<List<List<Weapon>>>();
			for (int i = 0; i < host.cardsInHand.Count; i++) {//sort out the weapons and foes
				if (host.cardsInHand [i] is Weapon) {
					if (host.cardsInHand [i].GetName () == "Sword")//temp set to 11 to distinguish from horse in the current sorting
						((Weapon)host.cardsInHand [i]).setBattlePoints (11);
					weaponList.Add ((Weapon)host.cardsInHand [i]);
				} else if (host.cardsInHand [i] is Foe) {
					if (host.cardsInHand [i].GetName () == linkedFoe)
						foeList.Add (new Pair<Foe,int> ((Foe)host.cardsInHand [i], ((Foe)host.cardsInHand [i]).GetBoostedBattlePoints ()));
					else
						foeList.Add (new Pair<Foe,int> ((Foe)host.cardsInHand [i], ((Foe)host.cardsInHand [i]).GetBattlePoints ()));
				} else if (host.cardsInHand [i] is Test && testCard == null) {
					testCard = (Test)host.cardsInHand [i];
				}
			}

			//Step 1, get every possible k-combination of every possible number of cards, all different objects are treated as different even if they are the same weapon - DONE
			//Note, same objects in memory not paired together, also if {2,1} exists {1,2} is not added and list are in ascending order of battle points
			//But remember for above all different objects are treated as different!! See test output in foreach loops below if not clear
			//Step 2 pass weaponComboList and foeList(not yet created) to makeQuest() (can specify difficulty)
			//Step 3 pick foes as per method and give them weapons, checking that an added weapon packed does not contain an already used weapon with ReferenceEquals()
			//Step 4 make sure all the enemies are stronger than the previous one, if not redo 3 until that is the case
			//Step 5 throw in a test card if necessary (it would be put in if it exists)
			for (int i = 0; i < weaponList.Count; i++) {//step 1
				weaponComboList.Add(K_Combinations.GetKCombs(weaponList,i+1));
				//Console.WriteLine  (K_Combinations.GetKCombs(weaponList,i+1).GetType());
			}

			for (int i = 0; i < weaponList.Count; i++) { //Resetting sword battle points to 10 now that possible comparisons with horse are done
				if (weaponList [i].GetName () == "Sword")
					weaponList [i].setBattlePoints (10);			
			}//That's the beautiful thing about object references and not copies
			Console.ReadLine ();
			foreach (var x in weaponComboList) { //FOR TESTING PURPOSES ONLY
				Console.WriteLine ("x count : " + x.Count);
				foreach (var y in x) {
					Console.WriteLine ("y count : " + y.Count);
					foreach (var z in y) {
						Console.WriteLine ("z : " + z);
					}
				}
			}
			foeList.Sort ((x, y) => x.Item2.CompareTo (y.Item2)); //sort by battle points(boosted or otherwise)
			foreach (var x in foeList) {
				Console.WriteLine (x.Item1 + "|" + x.Item2);
			}
			Console.WriteLine ("Difficulty? Easy or Hard");
			string difficulty = Console.ReadLine ();
			if(difficulty.ToLower() == "easy")
				makeQuest (stages, foeList, weaponComboList, testCard, 0, foeList.Count, (x,y) => x < y, x => x+1, numStages);
			else
				makeQuest (stages, foeList, weaponComboList, testCard, foeList.Count - 1, -1, (x,y) => x > y, x => x-1, numStages);
			return stages;
		}

		public void makeQuest(List<Pair<List<Adventure>,int>> stages, List<Pair<Foe,int>> foeList, List<List<List<Weapon>>> weaponComboList, Test testCard,
			int start, int finish, Func <int,int,bool> compar, Func <int,int> op, int numStages){
			bool foesFound = false;
			if (testCard != null)
				numStages--;
			
			for (int i = start; compar (i, finish); i = op (i)) {
				
				Random rand = new Random (Guid.NewGuid().GetHashCode()); //for 1st nested list
				Random randy = new Random (Guid.NewGuid().GetHashCode()); //for 2nd nested list
				Random unequipped = new Random(Guid.NewGuid().GetHashCode()); //odds an enemy doesn't grab a weapon at all
				//otherwise its all time dependent seeds and they'll all keep spitting out the same stuff, probably why I was having issues with randomness

				int x = rand.Next (0, weaponComboList.Count);
				int y = randy.Next (0, weaponComboList [x].Count);
				int z = unequipped.Next (0, 4); //see below, 25% chance of not getting a weapon

				Console.WriteLine ("x: " + x +  "| y : " + y); //FOR TESTING ONLY
				Console.WriteLine (weaponComboList.Count + " | " + weaponComboList[x].Count); //FOR TESTING ONLY

				if (weaponComboList [x].Count != 0 && z >= 1 ) {//because it can have an empty list in certain same weapon situations...
					stages.Add (new Pair<List<Adventure>, int> ((new List<Adventure> { foeList [i].Item1 }), foeList [i].Item2));
					stages [stages.Count - 1].Item1.AddRange (weaponComboList [x] [y]);
				} else { //so an enemy can not get a weapon as well, this is often needed otherwise we are stuck in an infinite loop
					stages.Add (new Pair<List<Adventure>, int> ((new List<Adventure> { foeList [i].Item1 }), foeList [i].Item2));
				}

				if (stages.Count == numStages) {
					foesFound = checkSelectedStageCards (stages);
					if (foesFound)
						break;
					else
						stages.Clear ();
				}
				if (!compar(op(i),finish) && !foesFound) { //we know we've reached the end and haven't found what we're looking for
					Console.WriteLine ("In the reset");
					if (start < finish)
						i = start - 1;
					else
						i = start + 1;
					stages.Clear (); //to prevent double allocation of foes
				}
			}

			Console.WriteLine ("Looks like we've found or foe stages, still need to add a test card (IF NEEDED)");
			Console.ReadLine ();
			if (testCard != null) {
				Random randA = new Random ();
				int a = randA.Next (0, stages.Count);
				stages.Insert (a, new Pair<List<Adventure>, int> (new List<Adventure> { testCard }, -1)); //Test does not have any battle points
			}

		}

		public bool checkSelectedStageCards(List<Pair<List<Adventure>,int>> stages){
			bool works = false;
			foreach (var cardsList in stages) {
				int tempBattlepointstotal = 0;
				foreach (var adventureCard in cardsList.Item1) {
					if(adventureCard is Weapon)//because Foe battle points have already been accounted for
						tempBattlepointstotal += adventureCard.GetBattlePoints ();
				}
				cardsList.Item2 += tempBattlepointstotal;
			}
			stages.Sort ((x, y) => x.Item2.CompareTo (y.Item2));
			foreach (var a in stages){//FOR TESTING ONLY
				foreach (var b in a.Item1) {
					Console.WriteLine (b);
				}
				Console.WriteLine ("Total: " + a.Item2);
			}
			Console.ReadLine ();
			List<int> battlePointValues = new List<int>();
			for (int i = 0; i < stages.Count; i++) {
				for (int y = i+1; y < stages.Count; y++) {
					bool containsCommonItem = stages[i].Item1.Any(x => stages[y].Item1.Any(z => ReferenceEquals(x,z)));
					if (containsCommonItem) {
						Console.WriteLine ("Common item found, re-doing stage making.");
						return false;
					}
				}
				battlePointValues.Add (stages [i].Item2);
			}

			foreach(var x in battlePointValues)
				Console.WriteLine ("In battlePointsValues: " + x);

			if (battlePointValues.Count > 0){
				var results = battlePointValues.GroupBy(i => i)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key);
				var duplicate = results.FirstOrDefault (); //might be more than one, but we dont care. One is enough to ruin it
				Console.WriteLine (duplicate);
				if (duplicate != 0)
					Console.WriteLine ("Same battle points for a stage, re-doing stage making. ");
				else
					works = true;
			}

			return works;
		}

		/*public List<Adventure> isValidQuest(string [] values, Player host, int numStages){ KEEPING HOST SELECTION OF QUEST CARDS TILL UNITY INTEGREATION.
		 * FOR NOW ALWAYS AUTO SELECT
		 * 
			List<Adventure> validatedCards = new List<Adventure> ();
			bool hostValidTest = true;
			for (int i = 0; i < values.Length; i++) { //Making sure values that aren't acceptable ints are stripped
				int temp = -1; 
				bool tempBool = int.TryParse (values [i], out temp);
				if (tempBool && temp > 0 && temp <= host.cardsInHand.Count) {
					validatedCards.Add (host.cardsInHand[temp - 1]);
				}
			}

			return validatedCards;
		} */

		public Player getHost(Quest storyCard){
			Player host = null;
			int tempPlayerTurn = playerTurn;
			for (int i = 0; i < numPlayers; i++) {
				if (tempPlayerTurn > numPlayers) {
					tempPlayerTurn = 1;
				}
				Console.WriteLine (playerList[tempPlayerTurn - 1].getName() + " would you like to host?");
				string userInput = Console.ReadLine ();
				if ((userInput == "y" || userInput == "Y") && canHost (tempPlayerTurn, storyCard.GetNumStages(), storyCard.GetLinkedFoe())) {
					Console.WriteLine ("Okay you can host");
					host = playerList [tempPlayerTurn - 1];
					break;
				}
				Console.WriteLine ("Chosen not to host and/or not able to host");
				tempPlayerTurn++;
			}

			return host;
		}

		public bool canHost(int tempPlayerTurn, int numStages, string linkedFoe){
			bool twinkies = false;
			Player p = playerList [tempPlayerTurn - 1];
			int testCounter = 0, foeCounter = 0; 

			List<Pair<Foe,int>> foes = new List<Pair<Foe,int>> ();
			List<Weapon> weapons = new List<Weapon> ();

			for (int i = 0; i < p.cardsInHand.Count; i++) {
				if (p.cardsInHand [i] is Foe) {
					if (p.cardsInHand [i].GetName () == linkedFoe) {
						foes.Add (new Pair<Foe,int> ((Foe)p.cardsInHand [i], ((Foe)p.cardsInHand [i]).GetBoostedBattlePoints ()));
						foeCounter++;
					} else {
						foes.Add (new Pair<Foe,int> ((Foe)p.cardsInHand [i], ((Foe)p.cardsInHand [i]).GetBattlePoints ()));
						foeCounter++;
					}
				} else if (p.cardsInHand [i] is Weapon) {
					weapons.Add ((Weapon)p.cardsInHand [i]);
				} else if (p.cardsInHand [i] is Test) {
					testCounter = 1;//can only use one test on a quest Dr.Suess
				}
			}
			var uniqueFoes = foes.GroupBy (f => f.Item2)			
				.Select (grp => grp.First ())
				.ToList (); //MAY have an exception here if no foe in list...not sure yet

			var uniqueWeapons = weapons.GroupBy (w => w.GetBattlePoints ())
				.Select (grp => grp.First ())
				.ToList (); //MAY have an exception here if no weapon in list...not sure yet

			for (int i = 0; i < uniqueFoes.Count; i++) { //FOR TESTING PURPOSES ONLY
				Console.WriteLine (i + ": " + uniqueFoes [i].Item1.GetName () + "|" + uniqueFoes [i].Item2);
			}
			for (int i = 0; i < uniqueWeapons.Count; i++) { //FOR TESTING PURPOSES ONLY
				Console.WriteLine (i + ": " + uniqueWeapons [i].GetName () + "|" + uniqueWeapons [i].GetBattlePoints ());
			}
			//Console.ReadLine ();
			if (numStages <= uniqueFoes.Count + uniqueWeapons.Count + testCounter && numStages <= foeCounter + testCounter) {
				twinkies = true;
			}

			return twinkies;
		}

		public void isEvent(Event storyCard){
			Console.WriteLine (storyCard.GetName() + ": " + storyCard.GetDescription());
			List<Player> playersTargeted = getPlayersTargeted (storyCard.GetWhosAffected());
			int tempPlayerturn = playerTurn;
			for (int i = 0; i < numPlayers; i++) { //add order here using tempPlayerTurn, playerTurn and checking if in list
				if (tempPlayerturn > numPlayers) {  
					tempPlayerturn = 1;
				}

				var results = from Player player in playersTargeted
				              where player == playerList [tempPlayerturn - 1]
				              select player;	  
				var successfulCandidate = results.FirstOrDefault();

				if (successfulCandidate != null) {
					Console.WriteLine ("Successful Candidate: " + successfulCandidate.getName());
					Player p = successfulCandidate;
					p.shields += storyCard.GetShieldModifier ();
					if (p.shields < 0)
						p.shields = 0;
					for (int y = 0; y < storyCard.GetAdventureCardModifier (); y++) {
						p.cardsInHand.Add ((Adventure)draw ("Adventure"));
					}
					if (storyCard.GetEliminateAllies ()) {
						p.alliesInPlay.Clear ();
					}
					if (storyCard.GetWeaponCardModifier () > 0) { //then we know its King's Call to Arms
						Console.WriteLine (p.getName () + " get rid of one weapon card, if that's not possible, get rid of two foe cards.");
						for (int y = 0; y < p.cardsInHand.Count; y++) {
							Console.WriteLine ("Card #" + (y + 1) + ": " + p.cardsInHand [y].GetName () + " with " + p.cardsInHand [y].GetBattlePoints () + " battle points.");
						}
						string cardsChosen = Console.ReadLine ();
						string[] values = cardsChosen.Split (',');
						validDiscardEvent (values, p, "Weapon");
					}
					//kings recognition is handled strictly in getPlayersTargeted and isQuest

					Console.WriteLine (p.getName () + ":");
					for (int y = 0; y < p.cardsInHand.Count; y++) {
						Console.WriteLine ("Card #" + (y + 1) + ": " + p.cardsInHand [y].GetName () + " with " + p.cardsInHand [y].GetBattlePoints () + " battle points.");
					}
					if (p.cardsInHand.Count > 12) {
						Console.WriteLine ("Alert, " + p.getName () + " you have more than 12 cards. Discard to (at least) 12 or be auto discarded.");
						Console.WriteLine ("Choose the cards you want to discard, seperated by commas. Invalid cards will be ignored.");
						string cardsChosen = Console.ReadLine ();
						string[] values = cardsChosen.Split (',');
						validDiscardEvent (values, p, "Adventure");
					}
				}
				tempPlayerturn++;
			}//end outer for
			stripCards ();
		}

		public void validDiscardEvent(string [] values, Player p, string type){
			List<int> validatedCards = new List<int> ();
			for (int i = 0; i < values.Length; i++) { //Making sure values that aren't acceptable ints are stripped
				int temp = -1; 
				bool tempBool = int.TryParse (values [i], out temp);
				if (tempBool && temp > 0 && temp <= p.cardsInHand.Count) {
					validatedCards.Add (temp);
				}
			}
			validatedCards.Sort ();
			if (type == "Adventure") {
				for (int i = validatedCards.Count - 1; i >= 0; i--) {
					Console.WriteLine (p.getName () + " discarded " + p.cardsInHand [validatedCards [i] - 1].GetName ());
					p.discardPile.Add (p.cardsInHand [validatedCards [i] - 1]);
					p.cardsInHand.RemoveAt (validatedCards [i] - 1);
				}//here they can get rid of more than two adventure cards if they want..but why would they? Also not getting rid of enough is handled later in stripCards()
			} else if (type == "Weapon") {//Gets rid of exactly one weapon card if possible. If more than one chosen, one closest to the back is gotten rid of
				bool oneWeapon = false;
				for (int i = validatedCards.Count - 1; i >= 0; i--) {
					if (p.cardsInHand [validatedCards [i] - 1] is Weapon) {
						oneWeapon = true;
						Console.WriteLine (p.getName () + " discarded " + p.cardsInHand [validatedCards [i] - 1].GetName ());
						p.discardPile.Add (p.cardsInHand [validatedCards [i] - 1]);
						p.cardsInHand.RemoveAt (validatedCards [i] - 1);
						break;
					}
				}
				if (!oneWeapon) {
					Console.WriteLine ("No valid weapon card specified, attempting to  auto-discard a weapon...");
					for (int i = p.cardsInHand.Count - 1; i >= 0; i--) {
						//Console.WriteLine ("WTF3: " + i);
						if (p.cardsInHand [i] is Weapon) {
							Console.WriteLine (p.getName () + " auto-discarded " + p.cardsInHand [i].GetName ());
							p.discardPile.Add (p.cardsInHand [i]);
							p.cardsInHand.RemoveAt (i);
							return;
						}
					}
					//Unrelated, any variable from an out scope is also known. Even if declared after in the method body
					Console.WriteLine ("Player has no weapon cards! Checking if specified input has foe card(s)");
					int foeDiscardCounter = 0; //Gets rid of up to two foe cards, if more than two are given, two closest to the back are taken away
					for (int i = validatedCards.Count - 1; i >= 0; i--) {
						if (foeDiscardCounter >= 2)
							break;
						if (p.cardsInHand [validatedCards [i] - 1] is Foe) {
							foeDiscardCounter++;
							Console.WriteLine (p.getName () + " discarded " + p.cardsInHand [validatedCards [i] - 1].GetName ());
							p.discardPile.Add (p.cardsInHand [validatedCards [i] - 1]);
							p.cardsInHand.RemoveAt (validatedCards [i] - 1);
						}
					}
					if (foeDiscardCounter < 2) {
						Console.WriteLine (foeDiscardCounter + " was discarded. Need to be two. Auto-discarding if possible...");
						for (int i = p.cardsInHand.Count - 1; i >= 0; i--) {
							if (foeDiscardCounter >= 2)
								break;
							if (p.cardsInHand [i] is Foe) {
								foeDiscardCounter++;
								Console.WriteLine (p.getName () + " auto-discarded " + p.cardsInHand [i].GetName ());
								p.discardPile.Add (p.cardsInHand [i]);
								p.cardsInHand.RemoveAt (i);
							}
						}
					}
				}
			} 
		}

		public List<Player> getPlayersTargeted(string whosAffected){ //this gets a list of whos affected, do something similar to temp player turn elsewhere to maintain discard order
			List<Player> p = new List<Player> ();
			if(whosAffected == Event.playersTargeted[0]){//LowestRankAndShield
				for (int i = 0; i < playerList.Count; i++) {
					if (i == 0) {
						p.Add (playerList [i]);
					}
					if (i != 0 && Player.rankDictionary[playerList [i].rank] < Player.rankDictionary[p [0].rank]) {//even if there's more than one player, it means they are same shield and rank so we can always compare with 0
						p.Clear ();
						p.Add (playerList [i]);
					}else if (i != 0 && playerList [i].rank == p [0].rank) {
						if (playerList [i].shields < p [0].shields) {
							p.Clear ();
							p.Add (playerList [i]);
						} else if (playerList [i].shields == p [0].shields) {
							p.Add (playerList [i]);
						}
					}
				}
			}else if(whosAffected == Event.playersTargeted[1]){//All
				for (int i = 0; i < playerList.Count; i++) {
					p.Add (playerList [i]);
				}
			}else if(whosAffected == Event.playersTargeted[2]){//AllExceptDrawer
				int tempPlayerTurn = playerTurn;
				for (int i = 0; i < playerList.Count; i++) {//Accessing raw player turn causes problems and shouldn't be used for indexing
					if (tempPlayerTurn > numPlayers)
						tempPlayerTurn = 1;
					if (i != (tempPlayerTurn - 1)) {
						p.Add (playerList [i]);
					}
				}
			}else if(whosAffected == Event.playersTargeted[3]){//DrawerOnly
				int tempPlayerTurn = playerTurn;
				if (tempPlayerTurn > numPlayers)//Accessing raw player turn causes problems and shouldn't be used for indexing
					tempPlayerTurn = 1;
				//Console.WriteLine (playerTurn - 1);
				p.Add (playerList[tempPlayerTurn - 1]);
			}else if(whosAffected == Event.playersTargeted[4]){//HighestRanked
				for (int i = 0; i < playerList.Count; i++) {
					if (i == 0) {
						p.Add (playerList [i]);
					}
					if (i != 0 && Player.rankDictionary[playerList [i].rank] > Player.rankDictionary[p [0].rank]) {
						p.Clear ();
						p.Add (playerList [i]);
					} else if (i != 0 && playerList [i].rank == p [0].rank) {
						p.Add (playerList [i]);
					}	
				}
			}else if(whosAffected == Event.playersTargeted[5]){//LowestRanked
				for (int i = 0; i < playerList.Count; i++) {
					if (i == 0) {
						p.Add (playerList [i]);
					}
					if (i != 0 && Player.rankDictionary[playerList [i].rank] < Player.rankDictionary[p [0].rank]) {
						p.Clear ();
						p.Add (playerList [i]);
					} else if (i != 0 && playerList [i].rank == p [0].rank) {
						p.Add (playerList [i]);
					}
				}
			}else if(whosAffected == Event.playersTargeted[6]){//Next
				QuestBonusShields += 2;
			}
			return p;
		}

		public void isTournament(Tournament storyCard, List<Player> tie = null, int eliminatedCompetitors = 0){
			string tournamentName = storyCard.GetName ();
			int bonusShields = storyCard.GetShieldModifier();
			List <Pair<Player, int>> tournamentEntry = new List <Pair<Player, int>> ();
			if (tie == null) {
				Console.WriteLine ("Tournament starting " + tournamentName + "!!! Players that wish to enter, type y, other for no\n");
				int tempPlayerTurn = playerTurn;
				for (int i = 0; i < numPlayers; i++) { //seeing which player wants to play
					if (tempPlayerTurn > numPlayers) {
						tempPlayerTurn = 1;
					}
					Console.Write (playerList [tempPlayerTurn - 1].getName () + "?: ");
					string userInput = Console.ReadLine ();
					Console.WriteLine ((tempPlayerTurn - 1) + "|" + (playerTurn - 1));
					Console.ReadLine ();
					if (userInput == "y" || userInput == "Y") {
						tournamentEntry.Add (new Pair<Player, int> (playerList [tempPlayerTurn - 1], 0));
					}
					tempPlayerTurn++;
				}
				//playerTurn++;
			} else {
				Console.WriteLine ("There's been a tie! Players will face off to decide the champion!");
				for (int i = 0; i < tie.Count; i++) {
					Console.WriteLine (tie[i].getName() + " will be participating");
					tournamentEntry.Add (new Pair<Player, int> (tie[i], 0));
				}
			}
			if (tournamentEntry.Count == 0) {
				Console.WriteLine ("No one entered the tournament");
				return;
			}
			if (tournamentEntry.Count == 1) {
				Console.WriteLine ("Congrats " + tournamentEntry[0].Item1.getName () + " you have won the tournament " + tournamentName + " and recieved " + (bonusShields+1) + " shields!");
				tournamentEntry [0].Item1.shields += bonusShields + 1;//cause only one player entered
				return;
			}
			for (int x = 0; x < tournamentEntry.Count; x++) { //FOR TESTING ONLY
				Console.WriteLine (tournamentEntry[x].Item1.getName() + " | " + tournamentEntry[x].Item1.cardsInHand.Count);
			}

			for (int i = 0; i < tournamentEntry.Count; i++) { //all players that wish to play get an additional adventure card. Assuming this is true for tiebreakes as well
				tournamentEntry [i].Item1.cardsInHand.Add ((Adventure)draw ("Adventure"));
			}

			for (int x = 0; x < tournamentEntry.Count; x++) { //FOR TESTING ONLY
				Console.WriteLine (tournamentEntry[x].Item1.getName() + " | " + tournamentEntry[x].Item1.cardsInHand.Count);
			}

			Console.ReadLine ();

			bonusShields += tournamentEntry.Count + eliminatedCompetitors;
			for (int i = 0; i < tournamentEntry.Count; i++) { //showing player the cards they have, making them choose which cards they want to play
				Player player = tournamentEntry[i].Item1;
				for (int y = 0; y < player.cardsInHand.Count; y++) {
					if(y == 0 && player.cardsInHand.Count > 12) 
						Console.WriteLine ("Alert, " + player.getName() + " you have more than 12 cards. Discard to (at least) 12 or be auto discarded.");
					Console.WriteLine ("Card #" + (y+1) + ": " + player.cardsInHand[y].GetName() + " with " + player.cardsInHand[y].GetBattlePoints() + " battle points.");
				}
				Console.WriteLine ("Choose the cards you want to use for the tournament by card#, seperated by commas. Invalid cards will be ignored.");
				string cardsChosen = Console.ReadLine ();
				string[] values = cardsChosen.Split (',');
				tournamentEntry[i].Item2 = validCards (values, player);
			}
			List<Player> winner = roundWinner (tournamentEntry);
			if (winner.Count > 1 && tie == null) {
				isTournament (storyCard, winner, (bonusShields - winner.Count)); //careful not to enter this loop twice, or it's inifinite loop time baby! At most one recursive call should happen
			}else if(winner.Count > 1 && tie != null){
				Console.WriteLine ("Tie breaker can't be decided, everyone gets shields!");
				for (int i = 0; i < winner.Count; i++) {
					winner [i].shields += bonusShields;
				}
			} else {
				Console.WriteLine ("Congrats " + winner[0].getName () + " you have won the tournament " + tournamentName + " and recieved " + bonusShields + " shields!");
				winner[0].shields += bonusShields;
			}
			stripCards (); //if player has more than 12 cards, time to get rid of them
		}

		public void stripCards() {
			for (int i = 0; i < playerList.Count; i++) {
				Player p = playerList [i];
				while (p.cardsInHand.Count > Player.MAX_CARDS_HAND) {
					Console.WriteLine ("Auto-removed " + p.cardsInHand[p.cardsInHand.Count - 1].GetName());
					p.discardPile.Add (p.cardsInHand[p.cardsInHand.Count - 1]);
					p.cardsInHand.RemoveAt (p.cardsInHand.Count - 1);
				}
			}
		}

		public List<Player> roundWinner(List<Pair<Player, int>> competitors){ //Return a winner, need to account for a draw
			List<Player> winner = new List<Player>(); //this should always return at least one person
			int winnerBP = 0;
			for (int i = 0; i < competitors.Count; i++) {
				if (i == 0) {
					winner.Add(competitors [i].Item1);
					winnerBP = competitors [i].Item2;
				}
				if (i != 0 && competitors [i].Item2 > winnerBP) {
					winner.Clear ();
					winner.Add (competitors [i].Item1);
					winnerBP = competitors [i].Item2;
				}else if(i != 0 && competitors [i].Item2 == winnerBP){
					winner.Add (competitors [i].Item1);
				}
			}
			return winner;
		} 

		public int validCards(string[] values, Player p, string quest = "no"){//Should be usable for tournament and quest
			List<int> validatedCards = new List<int> ();
			int battlePoints = 0;

			for (int i = 0; i < p.alliesInPlay.Count; i++) { //doing it up here so we don't double count ally battle points, as they are added into this array down below
				battlePoints += p.alliesInPlay [i].GetBattlePoints ();
			}

			Console.WriteLine ("WTF:" + values.Length);
			for (int i = 0; i < values.Length; i++) { //Making sure values that aren't acceptable ints are stripped
				int temp = -1; 
				bool tempBool = int.TryParse (values [i], out temp);
				if (tempBool && temp > 0 && temp <= p.cardsInHand.Count) {
					validatedCards.Add (temp);
				}
			}
			Console.WriteLine ("WTF#2 :" + validatedCards.Count);
			bool amourPlayer = false;
			bool firstamourSet = false;
			validatedCards.Sort(); //don't want to remove from the middle and mess up indexes
			for (int i = validatedCards.Count - 1; i >= 0; i--) { //No two weapons of the same type can be played, no foe cards, only one Amour
				if (p.cardsInHand [validatedCards [i] - 1] is Weapon) {
					for (int y = i - 1; y >= 0; y--) {
						Console.WriteLine (i + "|" + y); 
						Console.WriteLine ((p.cardsInHand [validatedCards [i] - 1]) + "|" + (p.cardsInHand [validatedCards [y] - 1]));
						if (p.cardsInHand [validatedCards [i] - 1] is Weapon && p.cardsInHand [validatedCards [y] - 1] is Weapon && ((Weapon)(p.cardsInHand [validatedCards [i] - 1])) == ((Weapon)(p.cardsInHand [validatedCards [y] - 1]))) {
							validatedCards.RemoveAt (y); //unintended bonus of removing duplicate of the same weapon (excact same object) added twice
							i = validatedCards.Count - 1; //if we remove from the end here and i doesn't decrement, we are in trouble
							Console.WriteLine ("WTF#2 :" + validatedCards.Count);
						}
					}
				} else if (p.cardsInHand [validatedCards [i] - 1] is Amour && !amourPlayer && quest == "no") {
					amourPlayer = true;
				} else if (p.cardsInHand [validatedCards [i] - 1] is Amour && amourPlayer && quest == "no") {
					validatedCards.RemoveAt (i);
				} else if (p.cardsInHand [validatedCards [i] - 1] is Amour && p.amourOnQuest && quest == "yes") {
					validatedCards.RemoveAt (i);
				}else if(p.cardsInHand [validatedCards [i] - 1] is Amour && !p.amourOnQuest && quest == "yes"){
					/*Console.WriteLine (p.getName() + " played(special quest amour case) " + p.cardsInHand[validatedCards [i] - 1].GetName());
					p.discardPile.Add (p.cardsInHand[validatedCards [i] - 1]);//dont want to double count amours anyway, also only one amour is allowed so additional amours are ignored
					p.cardsInHand.RemoveAt(validatedCards [i] - 1);
					validatedCards.RemoveAt (i);*/
					p.amourOnQuest = true;
					firstamourSet = true;
				} else if (p.cardsInHand [validatedCards [i] - 1] is Foe) {
					validatedCards.RemoveAt (i);
				} else if (p.cardsInHand [validatedCards [i] - 1] is Test) {
					validatedCards.RemoveAt (i);
				} else if(p.cardsInHand [validatedCards [i] - 1] is Ally){//allys stay on after
					p.alliesInPlay.Add((Ally)p.cardsInHand [validatedCards [i] - 1]);
				}
			}//end main for
			//So now all the indexes specified are valid inside of validated cards
			for (int i = validatedCards.Count - 1; i >= 0; i--) {
				battlePoints += p.cardsInHand [validatedCards [i] - 1].GetBattlePoints (); //because a player isn't going to see card #'s starting at 0, rather they start at 1
				Console.WriteLine (p.getName() + " played " + p.cardsInHand[validatedCards [i] - 1].GetName());
				p.discardPile.Add (p.cardsInHand[validatedCards [i] - 1]);
				p.cardsInHand.RemoveAt(validatedCards [i] - 1);
			}
			battlePoints += Player.rankDictionary[p.rank];
			if (quest == "yes" && p.amourOnQuest && !firstamourSet)
				battlePoints += 10; 
			Console.WriteLine ("Total battle points for player including rank " + battlePoints);
			return battlePoints;
		}


		/* INITIALIZATION FUNCTIONS */
		public void initializePlayers(){
			Console.WriteLine ("Please enter the number of players that wish to play:");
			string userInput = Console.ReadLine ();
			if (!(int.TryParse (userInput, out numPlayers) && numPlayers > 1 && numPlayers < 5)) {
				Console.WriteLine ("Invalid input, defaulting to four players");
				numPlayers = 4;
			}

			for (int i = 0; i < numPlayers; i++) {
				Console.WriteLine ("Player" + (i+1) + " please enter your name:");
				userInput = Console.ReadLine ();
				if (string.IsNullOrEmpty (userInput) || string.IsNullOrWhiteSpace (userInput)) {
					Console.WriteLine ("Player" + (i + 1) + " name set to 'Idiot who can't follow instructions #" + (i + 1) + "'");
					string name = "Idiot who can't follow instructions #" + i;
					playerList.Add (new Player (name));
				} else {
					Console.WriteLine ("Player" + (i + 1) +  " name set to " + userInput);
					playerList.Add (new Player (userInput));
				}
			}
		}
			
	    /* CARD OPERATION FUNCTIONS */
		public void deal(){
			for (int i = 0; i < 12; i++) {
				for (int y = 0; y < playerList.Count; y++) {
					playerList [y].cardsInHand.Add(adventureDeck[adventureDeck.Count-1]);
					adventureDeck.RemoveAt(adventureDeck.Count - 1);
				}
			}
		}

		public void shuffle(string whichDeck){
			Random rand = new Random ();
			switch (whichDeck) {
			case "storyDeck":
				for (int y = 0; y < 3; y++) {
					for (int i = 0; i < storyDeck.Count; i++) {
						//We should only ever shuffle a full story deck, for right now that's only at the start
						//Will have to deal with situation: All story cards are used, but no one has won
						int tempInt = rand.Next (0, storyDeck.Count); 
						Story swapCard = storyDeck [tempInt];
						storyDeck [tempInt] = storyDeck [i];
						storyDeck [i] = swapCard;
					}
				}
				break;
			default:
				for (int y = 0; y < 3; y++) {
					for (int i = 0; i < adventureDeck.Count; i++) {
						//Shuffling at the start is a full deck, but it could be a different number next time
						int tempInt = rand.Next(0, adventureDeck.Count);
						Adventure swapCard = adventureDeck [tempInt];
						adventureDeck [tempInt] = adventureDeck [i];
						adventureDeck [i] = swapCard;
					}
				}
				break;
			}
		}

		public Card draw(string whichDeck, int numCards = 1){
			if (storyDeck.Count == 0) {
				storyDeck = discardStoryDeck;
				shuffle ("storyDeck");
				discardStoryDeck = new List<Story> ();
			}
			if (adventureDeck.Count == 0) {
				Console.WriteLine ("Unhandled out of adventure cards case");
				Console.ReadLine ();
				//if we get here, need a case
			}
			switch (whichDeck) {
			case "Story":
				Console.WriteLine (storyDeck [storyDeck.Count - 1].GetName ());
				discardStoryDeck.Add (storyDeck [storyDeck.Count - 1]);
				storyDeck.RemoveAt (storyDeck.Count - 1);
				return discardStoryDeck [discardStoryDeck.Count - 1];
			default://For Adventure card drawing
				Console.WriteLine (adventureDeck [adventureDeck.Count - 1].GetName ());
				discardAdventureDeck.Add (adventureDeck [adventureDeck.Count - 1]); //Not sure if good use for this yet
				adventureDeck.RemoveAt (adventureDeck.Count - 1);
				return discardAdventureDeck [discardAdventureDeck.Count - 1];
			} 
		}
		/* CREATE DECK AND CARD FUNCTIONS */

		public void createDeck(){
			#region
			int cardCounter = 0;

			//ADVENTURE DECK

			/* AMOUR */

			for (int i = 0; i < 8; i++) {
				adventureDeck.Add(createAdventureCard ("Amour"));
				cardCounter++;
			}

			/* WEAPON */

			for (int i = cardCounter; i < 10; i++) {
				adventureDeck.Add(createAdventureCard("Weapon", "Excalibur", 30));
				cardCounter++;
			}

			for (int i = cardCounter; i < 16; i++) {
				adventureDeck.Add(createAdventureCard("Weapon", "Lance", 20));
				cardCounter++;
			}

			for (int i = cardCounter; i < 24; i++) { 
				adventureDeck.Add(createAdventureCard ("Weapon", "Battle-ax", 15));
				cardCounter++;
			}

			for (int i = cardCounter; i < 40; i++) { 
				adventureDeck.Add(createAdventureCard ("Weapon", "Sword", 10));
				cardCounter++;
			}

			for (int i = cardCounter; i < 51; i++) { 
				adventureDeck.Add(createAdventureCard ("Weapon", "Horse", 10));
				cardCounter++;
			}

			for (int i = cardCounter; i < 57; i++) { 
				adventureDeck.Add(createAdventureCard ("Weapon", "Dagger", 5));
				cardCounter++;
			}

			/* FOE */
			for(int i = cardCounter; i < 58; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Dragon", 50, 70));
				cardCounter++;
			}

			for(int i = cardCounter; i < 60; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Giant", 40, 40));
				cardCounter++;
			}

			for(int i = cardCounter; i < 64; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Mordred", 30, 30));
				cardCounter++;
			}

			for(int i = cardCounter; i < 66; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Green Knight", 25, 40));
				cardCounter++;
			}

			for(int i = cardCounter; i < 69; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Black Knight", 25, 35));
				cardCounter++;
			}

			for(int i = cardCounter; i < 75; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Evil Knight", 20, 30));
				cardCounter++;
			}

			for(int i = cardCounter; i < 83; i++){
						adventureDeck.Add(createAdventureCard ("Foe", "Saxon Knight", 15, 25));
				cardCounter++;
			}

			for(int i = cardCounter; i < 90; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Robber Knight", 15, 15));
				cardCounter++;
			}

			for(int i = cardCounter; i < 95; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Saxons", 10, 20));
				cardCounter++;
			}

			for(int i = cardCounter; i < 99; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Boar", 5, 15));
				cardCounter++;
			}

			for(int i = cardCounter; i < 107; i++){
				adventureDeck.Add(createAdventureCard ("Foe", "Thieves", 5, 5));
				cardCounter++;
			}
			
			/*TEST*/
			adventureDeck.Add(createAdventureCard ("Test", "Test of Valor", 0));
			adventureDeck.Add(createAdventureCard ("Test", "Test of Valor", 0));

			adventureDeck.Add(createAdventureCard ("Test","Test of Temptation", 0));
			adventureDeck.Add(createAdventureCard ("Test","Test of Temptation", 0));

			adventureDeck.Add(createAdventureCard ("Test", "Test of the Questing Beast", 0)); 
			adventureDeck.Add(createAdventureCard ("Test", "Test of the Questing Beast", 0));

			adventureDeck.Add(createAdventureCard ("Test", "Test of Morgan Le Fey", 3));
			adventureDeck.Add(createAdventureCard ("Test", "Test of Morgan Le Fey", 3));


			/*Allies*/
			adventureDeck.Add(createAdventureCard("Ally", "Sir Galahad", 15, 0));
			adventureDeck.Add(createAdventureCard("Ally", "Sir Gawain", 10, 0));
			adventureDeck.Add(createAdventureCard("Ally", "King Pellinore", 10, 0));
			adventureDeck.Add(createAdventureCard("Ally", "Sir Percival", 5, 0));
			adventureDeck.Add(createAdventureCard("Ally", "Sir Tristan", 10, 0));
			adventureDeck.Add(createAdventureCard("Ally", "King Arthur", 10, 2));
			adventureDeck.Add(createAdventureCard("Ally", "Queen Guinevere", 0, 3));
			adventureDeck.Add(createAdventureCard("Ally", "Merlin", 0, 0));
			adventureDeck.Add(createAdventureCard("Ally", "Queen Iseult", 0, 2));
			adventureDeck.Add(createAdventureCard("Ally", "Sir Lancelot", 15, 0));


			//STORY DECK
			cardCounter = 0;

			/*EVENT*/
			/*storyDeck.Add(createEventCard("Chivalrous Deed", 3, 0, false, 0, 0, Event.playersTargeted[0], "Player(s) with both lowest rank and least amount of shields, receives 3 shields"));
			storyDeck.Add(createEventCard("Pox", -1, 0, false, 0, 0, Event.playersTargeted[2], "All players except the player drawing this card lose 1 shield"));
			storyDeck.Add(createEventCard("Plague", -2, 0, false, 0, 0, Event.playersTargeted[3], "Drawer loses two shields if possible"));
			storyDeck.Add(createEventCard("King's Recognition", 2, 0, false, 0, 0, Event.playersTargeted[6], "The next player(s) to complete a Quest will receive 2 extra shields"));
			storyDeck.Add(createEventCard("King's Recognition", 2, 0, false, 0, 0, Event.playersTargeted[6], "The next player(s) to complete a Quest will receive 2 extra shields"));
			storyDeck.Add(createEventCard("Queen's Favor", 0, 2, false, 0, 0, Event.playersTargeted[5], "The lowest ranked player(s) immediately receives 2 Adventure Cards"));
			storyDeck.Add(createEventCard("Queen's Favor", 0, 2, false, 0, 0, Event.playersTargeted[5], "The lowest ranked player(s) immediately receives 2 Adventure Cards"));
			storyDeck.Add(createEventCard("Court Called to Camelot", 0, 0, true, 0, 0, Event.playersTargeted[1], "All Allies in play must be discarded"));
			storyDeck.Add(createEventCard("Court Called to Camelot", 0, 0, true, 0, 0, Event.playersTargeted[1], "All Allies in play must be discarded"));
			storyDeck.Add(createEventCard("King's Call To Arms", 0, 0, false, 1, 2, Event.playersTargeted[4], "The highest ranked player(s) must place 1 weapon in the discard pile. If unable to do so, 2 Foe Cards must be discarded"));
			storyDeck.Add(createEventCard("Prosperity Throughout the Realm", 0, 2, false, 0, 0, Event.playersTargeted[1], "All players may immeadiately draw two Adventure Cards"));

			/*Tournament*/
			/*storyDeck.Add(createTournamentCard("AT YORK", 0));
			storyDeck.Add(createTournamentCard("AT TINTAGEL", 1));
			storyDeck.Add(createTournamentCard("AT ORKNEY", 2));
			storyDeck.Add(createTournamentCard("AT CAMELOT", 3)); */

			/*Quest*/
			storyDeck.Add(createQuestCard("Journey through the Enchanted Forest", 3, "Evil Knight"));
			storyDeck.Add(createQuestCard("Vanquish King Arthur's Enemies", 3, "none"));
			storyDeck.Add(createQuestCard("Vanquish King Arthur's Enemies", 3, "none"));
			storyDeck.Add(createQuestCard("Repel the Saxon Raiders", 2, "All Saxons"));
			storyDeck.Add(createQuestCard("Repel the Saxon Raiders", 2, "All Saxons"));
			storyDeck.Add(createQuestCard("Boar Hunt", 2, "Boar"));
			storyDeck.Add(createQuestCard("Boar Hunt", 2, "Boar"));
			storyDeck.Add(createQuestCard("Search for the Questing Beast", 4, "none", "King Pellinore"));
			storyDeck.Add(createQuestCard("Defend the Queen's Honor", 4, "All", "Sir Lancelot"));
			storyDeck.Add(createQuestCard("Slay the Dragon", 3, "Dragon"));
			storyDeck.Add(createQuestCard("Rescue the Fair Maiden", 3, "Black Knight"));
			storyDeck.Add(createQuestCard("Search for the Holy Grail", 5, "All", "Sir Percival"));
			storyDeck.Add(createQuestCard("Test of the Green Knight", 4, "Green Knight", "Sir Gawain"));

			#endregion
		}

		public Story createEventCard(string name, int shieldModifier, int adventureCardModifier, bool eliminiateAllies, int weaponCardModifier, 
			int foeCardModifier, string whosAffected, string description){
			Event eve = new Event(name, shieldModifier, adventureCardModifier, eliminiateAllies, weaponCardModifier, foeCardModifier, whosAffected, description);
			return eve;
		}

		public Story createTournamentCard(string name, int shieldModifier){
			Tournament tournament = new Tournament(name, shieldModifier);
			return tournament;
		}

		public Story createQuestCard(string name, int numStages, string linkedFoe, string linkedAlly = "none"){
			Quest quest = new Quest(name, numStages, linkedFoe, linkedAlly);
			return quest;
		}

		public Adventure createAdventureCard(string type, string name = "", int battlePoints = 0, int bids = 0){
			switch (type) {
			case "Foe":
				Foe foe = new Foe (name, battlePoints, bids);//using bids as boosted battle points here..not good practice? 
				return foe;
			case "Weapon":
				Weapon weapon = new Weapon (name, battlePoints);
				return weapon;
			case "Ally":
				Ally ally = new Ally (name, battlePoints, bids);
				return ally;
			case "Amour":
				Amour amour = new Amour ();
				return amour;
			case "Test":
				Test test = new Test (name, bids);
				return test;
			default:
				Console.WriteLine ("Deck creation err, abort");
				Foe apocalypse = new Foe("Critical Err", 9001, -1);
				return apocalypse;
			}
		}
    }
}
