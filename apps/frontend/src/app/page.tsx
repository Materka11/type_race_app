import { Button } from "@heroui/button";
import { Card } from "@heroui/card";
import { Input } from "@heroui/input";
import { redirect } from "next/navigation";
import toast from "react-hot-toast";
import { v4 as uuidv4 } from "uuid";

export default function Home() {
  const createGame = async () => {
    "use server";

    const inviteCode = uuidv4();
    redirect(`/game/${inviteCode}`);
  };

  const joinGame = async (formData: FormData) => {
    "use server";

    const inviteCode = formData.get("invite_code") as string;

    if (!inviteCode) {
      toast.error("Invite code is required");
      return;
    }

    redirect(`/game/${inviteCode}`);
  };

  return (
    <main className="mx-auto w-full max-w-5xl p-5">
      <h1 className="mt-10 text-4xl font-bold">Type Race</h1>
      <p className="mt-5 text-lg text-gray-400">
        A simple yet addictive typing game designed to sharpen your speed, boost
        your accuracy, and test your focus under pressure. Whether you want to
        challenge your friends or push your solo high score, Type Race will keep
        you on your toes.
      </p>
      <Card className="mt-10 grid grid-cols-1 gap-5 p-5 md:grid-cols-2">
        <div className="flex flex-col justify-between p-5">
          <section>
            <h2 className="text-2xl font-medium">Create Game</h2>
            <p className="mt-5 text-gray-400">
              Start your own race and share the invite code with your friends.
              You’ll choose a paragraph, set a countdown, and watch the chaos
              unfold as everyone tries to finish first.
            </p>
          </section>
          <form action={createGame}>
            <Button type="submit" className="mt-5 w-full">
              Create Game
            </Button>
          </form>
        </div>

        <div className="flex flex-col justify-between p-5">
          <section>
            <h2 className="text-2xl font-medium">Join Game</h2>
            <p className="mt-5 text-gray-400">
              Already have a game code from a friend? Enter it below to jump
              right into the action. The race will begin once all players are
              ready.
            </p>
          </section>
          <section className="mt-5">
            <form action={joinGame}>
              <Input type="text" placeholder="Invite Code" name="invite_code" />
              <Button type="submit" className="mt-3 w-full">
                Join Game
              </Button>
            </form>
          </section>
        </div>
      </Card>
    </main>
  );
}
