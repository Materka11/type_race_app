"use client";

import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
} from "@microsoft/signalr";
import { useEffect, useRef, useState } from "react";
import toast from "react-hot-toast";

export enum GameStatus {
  NotStarted = 0,
  InProgress = 1,
  Finished = 2,
}

export function useGameHub(gameId: string, name: string) {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [players, setPlayers] = useState<IPlayer[]>([]);
  const [paragraph, setParagraph] = useState<string | null>(null);
  const [status, setStatus] = useState<GameStatus>(GameStatus.NotStarted);
  const [host, setHost] = useState<string>("");
  const [inputParagraph, setInputParagraph] = useState<string>("");
  const connectionRef = useRef<HubConnection | null>(null);
  const isConnectingRef = useRef<boolean>(false);

  const WEBSOCKET_URL = process.env.NEXT_PUBLIC_WEBSOCKET_URL as string;

  useEffect(() => {
    let isMounted = true;

    const connect = async () => {
      if (isConnectingRef.current) {
        console.log("Connection attempt already in progress, skipping");
        return;
      }

      if (connectionRef.current?.state === "Connected") {
        console.log("Connection already exists, skipping reconnect");
        return;
      }

      isConnectingRef.current = true;
      console.log("Starting new SignalR connection...");

      if (connectionRef.current) {
        console.log("Stopping existing connection");
        await connectionRef.current.stop();
        connectionRef.current = null;
      }

      const hubConnection = new HubConnectionBuilder()
        .withUrl(`${WEBSOCKET_URL}gamehub`)
        .configureLogging(LogLevel.Information)
        .withAutomaticReconnect()
        .build();

      hubConnection.on("error", (message) => {
        console.error("SignalR error:", message);
        toast.error("Error:", message);
      });

      hubConnection.on("player-joined", (player: IPlayer) => {
        console.log("Player joined:", player);
        setPlayers((prev) => {
          if (!prev.some((p) => p.id === player.id)) {
            return [...prev, player];
          }
          return prev;
        });
      });

      hubConnection.on("players", (playerList: IPlayer[]) => {
        console.log("Received players:", playerList);
        setPlayers(playerList);
      });

      hubConnection.on("player-left", (id) => {
        console.log("Player left:", id);
        setPlayers((prev) => prev.filter((p) => p.id !== id));
      });

      hubConnection.on(
        "player-score",
        ({
          id,
          score,
          precision,
        }: {
          id: string;
          score: number;
          precision: number;
        }) => {
          console.log("Player score update:", { id, score, precision });
          setPlayers((prev) =>
            prev.map((player) => {
              if (player.id === id) {
                return {
                  ...player,
                  score,
                  precision,
                };
              }
              return player;
            }),
          );
        },
      );

      hubConnection.on("game-started", (paragraph) => {
        console.log("Game started, paragraph:", paragraph);
        setParagraph(paragraph);
        setStatus(GameStatus.InProgress);
      });

      hubConnection.on("game-finished", () => {
        console.log("Game finished");
        setStatus(GameStatus.Finished);
        setInputParagraph("");
      });

      hubConnection.on("new-host", (id: string) => {
        console.log(
          "Received new-host event with id:",
          id,
          "current connectionId:",
          hubConnection.connectionId,
        );
        if (isMounted) {
          setHost(id);
        }
      });

      try {
        await hubConnection.start();
        console.log(
          "Connected to SignalR, connectionId:",
          hubConnection.connectionId,
        );
        await hubConnection.invoke("JoinGame", gameId, name);
        console.log("Joined game with gameId:", gameId, "name:", name);
        if (isMounted) {
          connectionRef.current = hubConnection;
          setConnection(hubConnection);
        }
      } catch (error) {
        console.error("Failed to connect to SignalR:", error);
        toast.error("Failed to connect to game server");
      }
    };

    connect();

    return () => {
      isMounted = false;
      if (connectionRef.current) {
        console.log("Cleaning up connection");
        connectionRef.current.stop();
        connectionRef.current = null;
      }
    };
  }, [gameId, name, WEBSOCKET_URL]);

  return {
    connection,
    players,
    paragraph,
    status,
    host,
    inputParagraph,
    setInputParagraph,
  };
}
