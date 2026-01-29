# Daily Rank Reward (Backend)

This repository does not contain server-side reward logic. The client code expects
an external Backend function that handles the daily top-1 reward at 18:00 KST.

## Scheduler (backend console)
- Run daily at 18:00 KST.
- Steps:
  1. Fetch top-1 player for the rank UUID used by the client.
  2. Store a reward row for the reward date (winner nickname/inDate, reward gold).
  3. Mark the reward as claimable for the winner only.

## Function contract
Function name: `DailyRankReward`

Request params:
```
{
  "action": "status" | "claim",
  "rankUuid": "<rank uuid>"
}
```

Status response:
```
{
  "claimable": true,
  "claimed": false,
  "rewardGold": 300,
  "winnerNickname": "PlayerName",
  "rewardDate": "yyyy-MM-dd",
  "message": ""
}
```

Claim response:
```
{
  "rewardGold": 300,
  "winnerNickname": "PlayerName",
  "rewardDate": "yyyy-MM-dd",
  "message": ""
}
```

Notes:
- Validate the caller is the reward owner before granting.
- Prevent duplicate claims for the same reward date.
