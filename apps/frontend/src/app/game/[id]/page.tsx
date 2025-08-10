import { Button } from "@heroui/button";
import { Card } from "@heroui/card";
import { Input } from "@heroui/input";
import { redirect } from "next/navigation";

interface IProps {
  searchParams?: Promise<{ name: string }>;
  params: Promise<{ id: string }>;
}

const GameIdPage = async ({ searchParams, params }: IProps) => {
  const { id } = await params;
  const username = (await searchParams)?.name;

  const appendName = async (formData: FormData) => {
    "use server";

    const name = formData.get("name") as string;

    if (!name) return;

    redirect(`/game/${id}?name=${name}`);
  };

  if (!username)
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
            <form className="mt-10" action={appendName}>
              <Input
                type="text"
                placeholder="Name"
                name="name"
                className="text-xl"
              />
              <Button type="submit" className="mt-5 w-full px-5 py-7 text-xl">
                Join Game
              </Button>
            </form>
          </section>
        </Card>
      </main>
    );

  return null;
};

export default GameIdPage;
