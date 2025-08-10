"use client";

import { GameStatus, useGameHub } from "@/hooks/useGameHub";
import { LeaderBoardCard } from "./LeaderBoardCard";
import { Button } from "@heroui/button";
import toast from "react-hot-toast";

interface IProps {
  name: string;
  gameId: string;
}

export const Game = ({ gameId, name }: IProps) => {
  const {
    connection,
    players,
    paragraph,
    status,
    host,
    inputParagraph,
    setInputParagraph,
  } = useGameHub(gameId, name);

  console.log("Host check:", {
    host,
    connectionId: connection?.connectionId,
    isHost: host === connection?.connectionId,
  });

  if (!connection) {
    return <div>Connecting to game...</div>;
  }

  const startGame = async () => {
    if (connection) {
      try {
        console.log(
          "Invoking StartGame with connectionId:",
          connection.connectionId,
        );
        await connection.invoke("StartGame");
      } catch (error) {
        console.error("Failed to start game:", error);
        toast.error("Failed to start game");
      }
    }
  };

  const handleTyping = async (typedText: string) => {
    if (connection && status === GameStatus.InProgress) {
      try {
        await connection.invoke("PlayerTyped", typedText);
      } catch (error) {
        console.error("Failed to send typed text:", error);
        toast.error("Failed to update score");
      }
    }
    setInputParagraph(typedText);
  };

  return (
    <div className="grid w-screen grid-cols-1 gap-20 p-10 lg:grid-cols-3">
      <div className="order-last w-full lg:order-first">
        <h2 className="mt-10 mb-10 text-2xl font-medium lg:mt-0">
          Leaderboard
        </h2>
        <div className="flex w-full flex-col gap-5">
          {players
            .sort((a, b) => b.score - a.score)
            .map((player, index) => (
              <LeaderBoardCard
                key={player.id}
                player={player}
                rank={index + 1}
              />
            ))}
        </div>
      </div>

      <div className="h-full lg:col-span-2">
        {status === GameStatus.NotStarted && (
          <div className="flex flex-col items-center justify-center p-10">
            <h2 className="text-2xl font-bold">
              Wating for players to join...
            </h2>
            <h3 className="text-lg text-gray-400">Invite Code: {gameId}</h3>
            {host === connection?.connectionId && (
              <Button className="mt-10 px-20" onClick={startGame}>
                Start Game
              </Button>
            )}
          </div>
        )}

        {status === GameStatus.InProgress && (
          <div className="h-full">
            <h1 className="mb-10 text-2xl font-bold">
              Type the paragraph below
            </h1>

            <div className="relative h-full">
              <p className="p-5 text-2xl opacity-75 lg:text-3xl">{paragraph}</p>
              <textarea
                value={inputParagraph}
                onChange={(e) => handleTyping(e.target.value)}
                className="absolute top-0 right-0 bottom-0 left-0 z-10 p-5 text-2xl outline-none lg:text-3xl"
                placeholder=""
                disabled={status !== GameStatus.InProgress || !connection}
              />
            </div>
          </div>
        )}

        {status === GameStatus.Finished && (
          <div className="flex flex-col items-center justify-center p-10">
            <h1>Game Finished</h1>
            {host === connection?.connectionId && (
              <Button className="mt-10 px-20" onClick={startGame}>
                Start Game
              </Button>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
