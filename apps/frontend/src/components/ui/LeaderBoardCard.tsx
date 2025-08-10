import { Card } from "@heroui/card";

interface IProps {
  player: IPlayer;
  rank: number;
}

export const LeaderBoardCard = ({ player, rank }: IProps) => {
  return (
    <Card className="flex w-full gap-5 p-5">
      <span className="text-xl"># {rank}</span>
      <span className="text-xl">{player.name}</span>
      <span className="ml-auto text-xl">{player.score}</span>
      <span className="ml-auto text-xl">{player.precision.toFixed(2)} %</span>
    </Card>
  );
};
