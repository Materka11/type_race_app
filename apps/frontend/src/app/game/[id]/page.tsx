"use client";

import { Game } from "@/components/ui/Game";
import { Button } from "@heroui/button";
import { Card } from "@heroui/card";
import { Input } from "@heroui/input";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { use } from "react";

interface IProps {
  params: Promise<{ id: string }>;
}

const GameIdPage = ({ params }: IProps) => {
  const router = useRouter();
  const searchParams = useSearchParams();
  const username = searchParams.get("name");
  const { id } = use(params);

  const [nameInput, setNameInput] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    console.log("GameIdPage rendered, username:", username, "gameId:", id);
  }, [username, id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!nameInput || isSubmitting) return;
    setIsSubmitting(true);
    console.log("Submitting name:", nameInput);
    router.push(`/game/${id}?name=${encodeURIComponent(nameInput)}`);
  };

  if (username) {
    return <Game gameId={id} name={username} />;
  }

  return (
    <main className="mx-auto mt-10 w-full max-w-5xl p-5">
      <Card className="flex w-full flex-col p-10">
        <section>
          <h2 className="text-4xl font-bold md:text-5xl">Enter your name</h2>
          <p className="mt-5 text-lg text-gray-400">
            Before you can join the race, we need to know who’s behind the
            keyboard. Your name will be displayed to other players during the
            game, so pick something fun, unique, or totally outrageous!
          </p>
        </section>
        <section>
          <form onSubmit={handleSubmit} className="mt-10">
            <Input
              type="text"
              placeholder="Name"
              value={nameInput}
              onChange={(e) => setNameInput(e.target.value)}
              className="text-xl"
            />
            <Button
              type="submit"
              className="mt-5 w-full px-5 py-7 text-xl"
              disabled={isSubmitting || !nameInput}
            >
              Join Game
            </Button>
          </form>
        </section>
      </Card>
    </main>
  );
};

export default GameIdPage;
